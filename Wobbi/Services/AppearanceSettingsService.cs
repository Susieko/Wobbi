using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Wobbi.Models;

namespace Wobbi.Services;

public sealed class AppearanceSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Wobbi", "appearance.json");

    private sealed class SettingsData
    {
        public string Appearance { get; set; } = nameof(WobbiAppearance.Classic);
        public string Personality { get; set; } = nameof(WobbiPersonality.Calm);
        public bool ShowSpeechBubbles { get; set; } = true;
        public bool ReducedAnimations { get; set; } = false;
        public bool MiniMode { get; set; } = false;

    }

    private static SettingsData ReadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return JsonSerializer.Deserialize<SettingsData>(
                    File.ReadAllText(SettingsPath)) ?? new SettingsData();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Debug.WriteLine($"Could not read Wobbi settings: {ex.Message}");
        }

        return new SettingsData();
    }

    private static void WriteSettings(SettingsData settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(
                settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Current in-memory selection still works if saving fails.
            Debug.WriteLine($"Could not save Wobbi settings: {ex.Message}");
        }
    }

    public WobbiAppearance Load()
    {
        string value = ReadSettings().Appearance;
        return Enum.TryParse<WobbiAppearance>(value, true, out var result)
               && Enum.IsDefined(result)
            ? result
            : WobbiAppearance.Classic;
    }

    public void Save(WobbiAppearance appearance)
    {
        SettingsData settings = ReadSettings();
        settings.Appearance = appearance.ToString();
        WriteSettings(settings);
    }

    public WobbiPersonality LoadPersonality()
    {
        string value = ReadSettings().Personality;
        return Enum.TryParse<WobbiPersonality>(value, true, out var result)
               && Enum.IsDefined(result)
            ? result
            : WobbiPersonality.Calm;
    }

    public void SavePersonality(WobbiPersonality personality)
    {
        SettingsData settings = ReadSettings();
        settings.Personality = personality.ToString();
        WriteSettings(settings);
    }

    public bool LoadShowSpeechBubbles() => ReadSettings().ShowSpeechBubbles;

    public void SaveShowSpeechBubbles(bool enabled)
    {
        SettingsData settings = ReadSettings();
        settings.ShowSpeechBubbles = enabled;
        WriteSettings(settings);
    }

    public bool LoadReducedAnimations() => ReadSettings().ReducedAnimations;

    public void SaveReducedAnimations(bool enabled)
    {
        SettingsData settings = ReadSettings();
        settings.ReducedAnimations = enabled;
        WriteSettings(settings);
    }

    public bool LoadMiniMode()
    {
        return ReadSettings().MiniMode;
    }

    public void SaveMiniMode(bool enabled)
    {
        SettingsData settings = ReadSettings();
        settings.MiniMode = enabled;
        WriteSettings(settings);
    }

}
