
using System;
using System.Collections.Generic;
using Dota2GSI;
using System.Threading;

namespace Wobbi.Services
{

    public enum DotaConnectionStatus
    {
        Waiting,
        Connected,
        InMatch
    }

    public enum DotaGameplayEvent
    {
        Kill,
        Death,
        DeathStreak,
        KillStreak,
        Victory,
        Defeat
    }

    public sealed class DotaGsiService : IDisposable
    {
        private readonly GameStateListener _listener;
        private readonly object _sync = new();

        // GSI connection tracking
        private string? _lastPhase;
        private bool _receivedFirstPacket;
        private bool _started;

        // Match tracking
        private bool _sawPreparation;
        private bool _matchInProgress;
        private bool _matchFinished;
        private DateTime? _postGameFirstSeenUtc;
        private long? _trackedMatchId;


        // Player team tracking
        private string? _lastKnownPlayerTeam;

        // Kill and death tracking
        private int? _lastKills;
        private int? _lastDeaths;

        // Streak tracking
        private int _deathsWithoutKill;
        private int _killsWithoutDeath;

        // Reaction cooldown
        private DateTime _lastReactionUtc =
            DateTime.MinValue;
        private DateTime _lastPacketUtc = DateTime.MinValue;

        private int _reactionCooldownSeconds = 10;

        public int ReactionCooldownSeconds
        {
            get => Volatile.Read(ref _reactionCooldownSeconds);

            set
            {
                if (value != 5 && value != 10 && value != 30)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value));
                }

                Volatile.Write(
                    ref _reactionCooldownSeconds,
                    value);
            }
        }

        // Events
        public event Action<string>? StatusChanged;

        public event Action<string>? ReactionRequested;

        public event Action<DotaGameplayEvent>?
            GameplayReactionRequested;

        public DotaGsiService()
        {
            // Must match the port in the Dota GSI config.
            _listener = new GameStateListener(34055);

            _listener.NewGameState += gameState =>
            {
                string phase =
                    gameState.Map?.GameState.ToString()
                    ?? "Unknown";

                var logs = new List<string>();

                string? reaction = null;

                DotaGameplayEvent? gameplayReaction = null;

                lock (_sync)
                {
                    _lastPacketUtc = DateTime.UtcNow;

                    // ----------------------------------
                    // MATCH SESSION TRACKING
                    // ----------------------------------

                    // Dota may report 0 when a match ID
                    // is not yet available.
                    long incomingMatchId =
                        gameState.Map?.MatchID ?? 0;

                    if (incomingMatchId > 0)
                    {
                        // A different valid match ID means
                        // we're observing a new match.
                        if (_trackedMatchId.HasValue &&
                            _trackedMatchId.Value != incomingMatchId)
                        {
                            logs.Add(
                                "New match detected! Resetting match state.");

                            _sawPreparation = false;
                            _matchInProgress = false;
                            _matchFinished = false;
                            _postGameFirstSeenUtc = null;

                            // Clear previous match statistics.
                            _lastKills = null;
                            _lastDeaths = null;

                            // Clear previous streaks.
                            _killsWithoutDeath = 0;
                            _deathsWithoutKill = 0;

                            // Forget the old player's team.
                            _lastKnownPlayerTeam = null;

                            // Reset ordinary reaction cooldown.
                            _lastReactionUtc = DateTime.MinValue;
                        }

                        // Only log the ID when it changes.
                        if (_trackedMatchId != incomingMatchId)
                        {
                            _trackedMatchId = incomingMatchId;

                            logs.Add(
                                $"Tracking Dota match: {_trackedMatchId}");
                        }
                    }

                    // ----------------------------------
                    // FIRST GSI PACKET
                    // ----------------------------------

                    if (!_receivedFirstPacket)
                    {
                        _receivedFirstPacket = true;

                        logs.Add(
                            "First Dota 2 GSI packet received!");

                        reaction =
                            "DOTA?! Habibs, are we winning " +
                            "or donating MMR today? 💜";
                    }

                    // ----------------------------------
                    // GAME PHASE CHANGES
                    // ----------------------------------

                    if (_lastPhase != phase)
                    {
                        _lastPhase = phase;

                        logs.Add(
                            $"Game phase: {phase}");
                    }

                    bool preparing =
                        phase is
                        "DOTA_GAMERULES_STATE_HERO_SELECTION" or
                        "DOTA_GAMERULES_STATE_STRATEGY_TIME" or
                        "DOTA_GAMERULES_STATE_PRE_GAME";

                    bool playing =
                        phase ==
                        "DOTA_GAMERULES_STATE_GAME_IN_PROGRESS";

                    bool postGame =
                        phase ==
                        "DOTA_GAMERULES_STATE_POST_GAME";

                    // ----------------------------------
                    // NEW MATCH PREPARATION
                    // ----------------------------------

                    if (preparing)
                    {
                        // Reset only when entering preparation
                        // for a newly observed match.
                        if (!_sawPreparation || _matchFinished)
                        {
                            _lastKills = null;
                            _lastDeaths = null;

                            _deathsWithoutKill = 0;
                            _killsWithoutDeath = 0;

                            _lastKnownPlayerTeam = null;

                            _lastReactionUtc =
                                DateTime.MinValue;
                        }

                        _sawPreparation = true;
                        _matchInProgress = false;
                        _matchFinished = false;
                    }

                    // ----------------------------------
                    // MATCH START
                    // ----------------------------------

                    if (playing &&
                        !_matchInProgress &&
                        !_matchFinished)
                    {
                        _matchInProgress = true;

                        logs.Add(_sawPreparation
                            ? "MATCH STARTED! 🎮"
                            : "Ongoing match detected! 🎮");

                        reaction =
                            "IT'S GAME TIME!! 🎮💜 " +
                            "Show them what you've got!";
                    }

                    // ----------------------------------
                    // PLAYER TEAM TRACKING
                    // ----------------------------------

                    if (playing &&
                        gameState.Player?.LocalPlayer
                            is { } localPlayer)
                    {
                        string team =
                            localPlayer.Team.ToString();

                        if (NormalizeDotaTeam(team) != null)
                        {
                            _lastKnownPlayerTeam = team;
                        }
                    }


                    // ----------------------------------
                    // MATCH FINISH
                    // ----------------------------------

                    if (postGame &&
                        _matchInProgress &&
                        !_matchFinished)
                    {
                        DateTime now = DateTime.UtcNow;

                        // First post-game packet.
                        if (!_postGameFirstSeenUtc.HasValue)
                        {
                            _postGameFirstSeenUtc = now;

                            logs.Add(
                                "MATCH FINISHED! 🏁 " +
                                "Waiting for official result...");
                        }

                        // Winning team reported by Dota.
                        string? winningTeamRaw =
                            gameState.Map?
                                .WinningTeam.ToString();

                        // Current player's team.
                        string? playerTeamRaw =
                            gameState.Player?
                                .LocalPlayer?
                                .Team.ToString();

                        // Use the last known team when
                        // post-game player data is missing.
                        string? playerTeam =
                            NormalizeDotaTeam(playerTeamRaw)
                            ?? NormalizeDotaTeam(
                                _lastKnownPlayerTeam);

                        string? winningTeam =
                            NormalizeDotaTeam(winningTeamRaw);

                        bool resultAvailable =
                            winningTeam != null &&
                            playerTeam != null;

                        bool timedOut =
                            now - _postGameFirstSeenUtc.Value
                            >= TimeSpan.FromSeconds(30);

                        if (resultAvailable || timedOut)
                        {
                            if (resultAvailable)
                            {
                                gameplayReaction =
                                    winningTeam == playerTeam
                                        ? DotaGameplayEvent.Victory
                                        : DotaGameplayEvent.Defeat;

                                logs.Add(
                                    $"Match result: {gameplayReaction}");
                            }
                            else
                            {
                                // No reliable winner was received.
                                // Don't guess who won.
                                reaction =
                                    "Match over, habibs! 💜 " +
                                    "Let's see how we did!";

                                logs.Add(
                                    "Match result unavailable.");
                            }

                            // Finish this session once.
                            _matchInProgress = false;
                            _matchFinished = true;
                            _sawPreparation = false;

                            _postGameFirstSeenUtc = null;

                            // Clear match-specific state.
                            _lastKills = null;
                            _lastDeaths = null;

                            _killsWithoutDeath = 0;
                            _deathsWithoutKill = 0;

                            _lastKnownPlayerTeam = null;
                        }
                    }


                    // ----------------------------------
                    // KILL AND DEATH TRACKING
                    // ----------------------------------

                    if (playing &&
                        gameState.Player?.LocalPlayer
                            is { } player)
                    {
                        int kills = player.Kills;
                        int deaths = player.Deaths;

                        // Calculate changes before updating
                        // the previous snapshot.
                        int killsGained = _lastKills.HasValue
                            ? Math.Max(
                                0,
                                kills - _lastKills.Value)
                            : 0;

                        int deathsGained = _lastDeaths.HasValue
                            ? Math.Max(
                                0,
                                deaths - _lastDeaths.Value)
                            : 0;

                        bool newKill = killsGained > 0;
                        bool newDeath = deathsGained > 0;

                        // ----------------------------------
                        // NEW KILLS
                        // ----------------------------------

                        if (newKill)
                        {
                            logs.Add(
                                $"KILL! +{killsGained} | " +
                                $"Total kills: {kills}");
                        }

                        // ----------------------------------
                        // NEW DEATHS
                        // ----------------------------------

                        if (newDeath)
                        {
                            logs.Add(
                                $"DEATH! +{deathsGained} | " +
                                $"Total deaths: {deaths}");
                        }

                        // ----------------------------------
                        // DEATH STREAK TRACKING
                        // ----------------------------------

                        bool deathStreakReached = false;

                        if (newKill)
                        {
                            // Getting a kill resets the
                            // deaths-without-kill counter.
                            _deathsWithoutKill = 0;
                        }
                        else if (newDeath)
                        {
                            int previousStreak =
                                _deathsWithoutKill;

                            _deathsWithoutKill +=
                                deathsGained;

                            // Trigger once when crossing 3.
                            deathStreakReached =
                                previousStreak < 3 &&
                                _deathsWithoutKill >= 3;

                            logs.Add(
                                $"Deaths without kill: " +
                                $"{_deathsWithoutKill}");
                        }

                        // ----------------------------------
                        // KILL STREAK TRACKING
                        // ----------------------------------

                        bool killStreakReached = false;

                        if (newDeath)
                        {
                            // Dying resets the
                            // kills-without-death counter.
                            _killsWithoutDeath = 0;
                        }
                        else if (newKill)
                        {
                            int previousStreak =
                                _killsWithoutDeath;

                            _killsWithoutDeath +=
                                killsGained;

                            // Celebrate once when crossing 3.
                            killStreakReached =
                                previousStreak < 3 &&
                                _killsWithoutDeath >= 3;

                            logs.Add(
                                $"Kills without death: " +
                                $"{_killsWithoutDeath}");
                        }

                        // ----------------------------------
                        // UPDATE STAT BASELINES
                        // ----------------------------------

                        // Never decrease the baseline during
                        // the same observed match.
                        _lastKills = _lastKills.HasValue
                            ? Math.Max(
                                _lastKills.Value,
                                kills)
                            : kills;

                        _lastDeaths = _lastDeaths.HasValue
                            ? Math.Max(
                                _lastDeaths.Value,
                                deaths)
                            : deaths;

                        // ----------------------------------
                        // GAMEPLAY REACTIONS
                        // ----------------------------------

                        if (newKill || newDeath)
                        {
                            DateTime now = DateTime.UtcNow;

                            double secondsSinceLast =
                                (now - _lastReactionUtc)
                                .TotalSeconds;

                            // Streak milestones bypass
                            // the normal cooldown.
                            bool milestone =
                                deathStreakReached ||
                                killStreakReached;

                            if (milestone ||
    secondsSinceLast >= ReactionCooldownSeconds)
                            {
                                _lastReactionUtc = now;

                                gameplayReaction =
                                    deathStreakReached
                                        ? DotaGameplayEvent.DeathStreak
                                        : killStreakReached
                                            ? DotaGameplayEvent.KillStreak
                                            : newDeath
                                                ? DotaGameplayEvent.Death
                                                : DotaGameplayEvent.Kill;

                                logs.Add(
    $"Gameplay reaction skipped: " +
    $"{ReactionCooldownSeconds}-second cooldown.");
                            }
                            else
                            {
                                logs.Add(
                                    "Gameplay reaction skipped: " +
                                    "10-second cooldown.");
                            }
                        }
                    }

                    // Disconnects or Undefined phases do
                    // not count as completed matches.
                }

                // ----------------------------------
                // DISPATCH EVENTS
                // ----------------------------------

                // Send diagnostics outside the lock.
                foreach (string log in logs)
                {
                    StatusChanged?.Invoke(log);
                }

                // General speech reactions:
                // greeting, match start, unknown result.
                if (reaction != null)
                {
                    StatusChanged?.Invoke(
                        $"Sending reaction: {reaction}");

                    ReactionRequested?.Invoke(reaction);
                }

                // Personality-based reactions:
                // kills, deaths, streaks, wins, losses.
                if (gameplayReaction.HasValue)
                {
                    GameplayReactionRequested?.Invoke(
                        gameplayReaction.Value);
                }
            };
        }

        // ----------------------------------
        // TEAM NORMALIZATION
        // ----------------------------------

        private static string? NormalizeDotaTeam(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string team = value.Trim()
                .ToUpperInvariant()
                .Replace("_", "");

            if (team.Contains("RADIANT") ||
                team.Contains("GOODGUYS") ||
                team is "TEAM2" or "2")
            {
                return "Radiant";
            }

            if (team.Contains("DIRE") ||
                team.Contains("BADGUYS") ||
                team is "TEAM3" or "3")
            {
                return "Dire";
            }

            return null;
        }

        // ----------------------------------
        // START GSI LISTENER
        // ----------------------------------

        public bool Start()
        {
            if (_started)
                return true;

            try
            {
                _started = _listener.Start();

                StatusChanged?.Invoke(_started
                    ? "Listening on localhost:34055"
                    : "Could not start GSI listener.");

                return _started;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(
                    $"GSI startup error: {ex.Message}");

                return false;
            }
        }

        // ----------------------------------
        // CLEAN UP
        // ----------------------------------

        public void Dispose()
        {
            _listener.Stop();
            _listener.Dispose();
        }


        public DotaConnectionStatus GetConnectionStatus()
        {
            lock (_sync)
            {
                // No data yet, or no recent packets.
                // Our GSI config normally sends heartbeats
                // approximately every 10 seconds.
                if (_lastPacketUtc == DateTime.MinValue ||
                    DateTime.UtcNow - _lastPacketUtc >
                        TimeSpan.FromSeconds(30))
                {
                    return DotaConnectionStatus.Waiting;
                }

                // An actively running game.
                if (_lastPhase ==
                    "DOTA_GAMERULES_STATE_GAME_IN_PROGRESS")
                {
                    return DotaConnectionStatus.InMatch;
                }

                // Disconnects and undefined states are not
                // evidence of an active GSI connection.
                if (_lastPhase is null or
                    "Undefined" or
                    "Unknown" or
                    "DOTA_GAMERULES_STATE_DISCONNECT")
                {
                    return DotaConnectionStatus.Waiting;
                }

                // Receiving another valid game phase.
                return DotaConnectionStatus.Connected;
            }
        }

    }
}
