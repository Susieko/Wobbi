
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wobbi.Services;
using System.Windows.Controls;
using Wobbi.Models;
using Wobbi.Animations;
using System.Windows.Media.Imaging;
namespace Wobbi;
using System.IO;
using Wobbi.Models;


public partial class MainWindow : Window
{
    private ReminderCenterWindow? _reminderCenter;
    private ReminderToastWindow? _reminderToast;
    private readonly DispatcherTimer _blinkTimer = new();
    private readonly Random _random = new();
    private readonly TranslateTransform _floating = new();
    private readonly TranslateTransform _petBounce = new();
    private readonly RotateTransform _curiousTilt = new();
    private readonly DispatcherTimer _miniGlanceTimer = new();
    private readonly AgendaService _agendaService = new();
    private bool _miniLookingAround = false;
    private readonly DispatcherTimer _miniClickTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(550)
    };

    private bool _isBlinking = false;
    private bool _isSleeping = false;
    private bool _isMiniMode = false;
    private double _fullModeLeft;
    private double _fullModeTop;

    private readonly DispatcherTimer _miniBlinkTimer = new();
    private bool _miniBlinking = false;


    private readonly DispatcherTimer _miniAgendaPopupTimer = new()
    {
        Interval = TimeSpan.FromSeconds(8)
    };


    private readonly DispatcherTimer _autoSnoozeTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };


    private readonly DispatcherTimer _sleepyScritchTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };


    private bool _isAutoSnoozing = false;

    private bool _autoSnoozeEnabled = true;
    private AgendaReminderService? _agendaReminders;


    private readonly DispatcherTimer _miniWakeBubbleTimer = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };


    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        // Double-click switches between Mini and Full Mode.
        if (e.ClickCount == 2)
        {
            _miniClickTimer.Stop();

            ToggleMiniMode_Click(sender, e);

            e.Handled = true;
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        // Remember where Wobbi was before the mouse action.
        double startLeft = Left;
        double startTop = Top;

        // Keep dragging functionality.
        DragMove();

        // Did Wobbi actually move?
        bool wasDragged =
            Math.Abs(Left - startLeft) > 3 ||
            Math.Abs(Top - startTop) > 3;

        // Only a stationary click in Mini Mode opens chat.
        if (_isMiniMode && !wasDragged)
        {
            _miniClickTimer.Stop();
            _miniClickTimer.Start();
        }
    }




    private void HeadPatZone_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_isMiniMode || e.ChangedButton != MouseButton.Left)
            return;

        // Keep double-click Mini Mode working.
        if (e.ClickCount == 2)
            return;

        e.Handled = true;

        _isPatting = true;
        _isHoldingPat = false;

        // Hand changes to pressed pose.
        HeadPatZone.Cursor = _patPressedCursor;

        // Keep receiving mouse events until release.
        if (!HeadPatZone.CaptureMouse())
        {
            CancelPatGesture();
            return;
        }

        // Begin detecting a longer head scritch.
        _patHoldTimer.Stop();
        _patHoldTimer.Start();
    }


    private void PatHoldTimer_Tick(
        object? sender,
        EventArgs e)
    {
        _patHoldTimer.Stop();

        if (!_isPatting ||
            Mouse.LeftButton != MouseButtonState.Pressed)
            return;

        // Wobbi is receiving head scritchies!
        _isHoldingPat = true;

        HeadPatZone.Cursor = _patHoldingCursor;
        StartHeadScritchies();
    }

    private void HeadPatZone_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)

    {
        if (!_isPatting)
            return;

        e.Handled = true;

        bool wasHolding = _isHoldingPat;

        CancelPatGesture();

        // A short click is a regular happy pat.
        if (!wasHolding && !_isSleeping)
        {
            PetWobbi_Click(sender, e);
        }

        if (wasHolding && !_isSleeping && !_reducedAnimations)
        {
            PlayScritchReleaseBounce();
        }


        // Long-hold reactions arrive in Step 4.
    }




    private void CancelPatGesture()
    {
        bool wasHolding = _isHoldingPat;

        _patHoldTimer.Stop();

        _isPatting = false;
        _isHoldingPat = false;

        HeadPatZone.Cursor = _patHoverCursor;

        // Return to normal when the hold ends.
        if (wasHolding)
            StopHeadScritchies();

        if (HeadPatZone.IsMouseCaptured)
            HeadPatZone.ReleaseMouseCapture();
    }


    private void HeadPatZone_LostMouseCapture(
        object sender,
        MouseEventArgs e)
    {
        // Prevent a stuck patting state if
        // mouse capture is interrupted.
        if (_isPatting)
            CancelPatGesture();
    }


    private ChatWindow? _chatWindow;

    // The same speech bubble is used for replies and animated thinking dots.
    private readonly DispatcherTimer _thinkingTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(380)
    };
    private bool _isThinking;
    private int _thinkingDotCount;
    private readonly ScaleTransform _breathing = new(1, 1);
    // Used to hide reply bubbles without an opacity animation in Reduced Animations mode.
    private readonly DispatcherTimer _replyHideTimer = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };
    private readonly DispatcherTimer _happyResetTimer = new();
    private bool _isHappy = false;
    private readonly AppearanceSettingsService _appearanceSettings = new();
    private WobbiAppearance _appearance = WobbiAppearance.Classic;
    private WobbiPersonality _personality = WobbiPersonality.Calm;
    private bool _showSpeechBubbles = true;
    private bool _reducedAnimations = false;

    private ImageSource? _cosmicIdleSource;
    private BitmapImage? _cosmicBlinkSource;

    private BitmapImage? _cosmicHappySource;

    private BitmapImage? _cosmicScritchSource;




    // Wobbi v0.4.1 — Head scritchies
    private Cursor? _patHoverCursor;
    private Cursor? _patPressedCursor;
    private Cursor? _patHoldingCursor;

    private bool _isPatting;
    private bool _isHoldingPat;

    private readonly DispatcherTimer _patHoldTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(350)
    };
    private AgendaWindow? _agendaWindow;

    public MainWindow()
    {
        InitializeComponent();


        _miniAgendaPopupTimer.Tick += (_, _) =>
        {
            _miniAgendaPopupTimer.Stop();
            MiniAgendaPopup.IsOpen = false;
        };

        Closed += (_, _) => _miniAgendaPopupTimer.Stop();

        _agendaReminders = new AgendaReminderService(
            _agendaService,
            ShowAgendaReminder);

        Loaded += (_, _) => _agendaReminders.Start();

        Closed += (_, _) => _agendaReminders.Stop();



        // Wobbi v0.4.1 — Custom patting cursor

        string cursorPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Cursors",
            "wobbi-pat-hover-reference.cur"
        );

        if (!File.Exists(cursorPath))
        {
            throw new FileNotFoundException(
                "Wobbi's patting cursor is missing!",
                cursorPath
            );
        }


        // Load all three cursor poses
        _patHoverCursor = new Cursor(cursorPath);

        _patPressedCursor = new Cursor(Path.Combine(
            AppContext.BaseDirectory,
            "Assets", "Cursors",
            "wobbi-pat-pressed-reference.cur"
        ));

        _patHoldingCursor = new Cursor(Path.Combine(
            AppContext.BaseDirectory,
            "Assets", "Cursors",
            "wobbi-pat-holding-reference.cur"
        ));

        HeadPatZone.Cursor = _patHoverCursor;

        _patHoldTimer.Tick += PatHoldTimer_Tick;


        WindowStartupLocation = WindowStartupLocation.Manual;
        Loaded += MainWindow_Loaded;
        // Read preferences before starting Wobbi's idle animations.
        _showSpeechBubbles = _appearanceSettings.LoadShowSpeechBubbles();
        _reducedAnimations = _appearanceSettings.LoadReducedAnimations();

        _autoSnoozeEnabled = _appearanceSettings.LoadAutoSnooze();


        var transforms = new TransformGroup();
        transforms.Children.Add(_breathing);
        transforms.Children.Add(_floating);
        transforms.Children.Add(_petBounce);
        transforms.Children.Add(_curiousTilt);

        WobbiImage.RenderTransform = transforms;
        WobbiImage.RenderTransformOrigin = new Point(0.5, 0.85);



        UpdateFloatingAnimation();

        UpdateBreathing();

        _blinkTimer.Interval = TimeSpan.FromSeconds(3);
        _blinkTimer.Tick += BlinkTimer_Tick;
        _blinkTimer.Start();
        _happyResetTimer.Interval = TimeSpan.FromSeconds(2.0);
        _happyResetTimer.Tick += HappyResetTimer_Tick;
        _thinkingTimer.Tick += ThinkingTimer_Tick;
        _replyHideTimer.Tick += ReplyHideTimer_Tick;
        _miniClickTimer.Tick += MiniClickTimer_Tick;

        _miniBlinkTimer.Interval = TimeSpan.FromSeconds(4);
        _miniBlinkTimer.Tick += MiniBlinkTimer_Tick;
        _miniBlinkTimer.Start();
        _miniGlanceTimer.Interval = TimeSpan.FromSeconds(8);
        _miniGlanceTimer.Tick += MiniGlanceTimer_Tick;
        _miniGlanceTimer.Start();
        _sleepyScritchTimer.Tick += SleepyScritchTimer_Tick;


        _autoSnoozeTimer.Tick += (_, _) =>
        {
            // Don't automatically sleep or wake when disabled.
            if (!_autoSnoozeEnabled)
                return;

            TimeSpan? idleTime = WindowsIdleService.GetIdleTime();


            if (idleTime == null)
                return;

            // Fall asleep after 5 minutes of inactivity.
            if (idleTime.Value >= TimeSpan.FromMinutes(5))
            {
                if (!_isSleeping)
                {
                    _isAutoSnoozing = true;
                    ToggleSleep();
                }
            }
            // Wake only if the nap started automatically.

            else if (_isAutoSnoozing)
            {
                _isAutoSnoozing = false;


                if (_isSleeping)
                {
                    ToggleSleep();
                    PlayAutoWakeHop();

                    if (_isMiniMode)
                    {
                        if (_showSpeechBubbles)
                        {
                            _miniWakeBubbleTimer.Stop();
                            MiniWakePopup.IsOpen = true;
                            _miniWakeBubbleTimer.Start();
                        }
                    }
                    else
                    {
                        ShowSpeechBubble("Oh! You're back! 💜");
                    }
                }


            }

        };

        _autoSnoozeTimer.Start();

        _miniWakeBubbleTimer.Tick += (_, _) =>
        {
            _miniWakeBubbleTimer.Stop();
            MiniWakePopup.IsOpen = false;
        };

        _appearance = _appearanceSettings.Load();
        ApplyAppearance();
        _personality = _appearanceSettings.LoadPersonality();
        ApplyPersonalityMenu();
        ApplyPreferencesMenu();
    }


private void RefreshReminderBadge()
    {
        MiniAgendaBadge.Visibility =
            _agendaService.HasPendingReminders()
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void OpenReminders_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowReminderCenter();
    }

    private void ShowReminderCenter()
    {
        if (_reminderCenter != null)
        {
            _reminderCenter.Refresh();

            if (_reminderCenter.WindowState ==
                WindowState.Minimized)
            {
                _reminderCenter.WindowState =
                    WindowState.Normal;
            }

            _reminderCenter.Activate();
            return;
        }

        var window = new ReminderCenterWindow(
            _agendaService);

        _reminderCenter = window;

        window.RemindersChanged += (_, _) =>
        {
            RefreshReminderBadge();
        };

        window.Closed += (_, _) =>
        {
            _reminderCenter = null;
        };

        window.Show();
        window.Activate();
    }


    private void StartHeadScritchies()
    {
        // Don't let an earlier happy-pat timer
        // interrupt the scritching expression.
        _happyResetTimer.Stop();
        // Cancel any previous lingering sleepy smile.
        _sleepyScritchTimer.Stop();
        StopHeartParticles();

        // Stop any previous bounce.
        _petBounce.BeginAnimation(
            TranslateTransform.YProperty, null);
        _petBounce.Y = 0;

        // Show sleepy closed eyes with our new smile.
        OpenEyes.Visibility = Visibility.Collapsed;
        ClosedEyes.Visibility = Visibility.Collapsed;
        HappyEyes.Visibility = Visibility.Collapsed;
        SleepEyes.Visibility = Visibility.Collapsed;
        ScritchEyes.Visibility = Visibility.Visible;

        // Prevent normal blinking during scritchies.
        _isHappy = true;


        if (_appearance == WobbiAppearance.Cosmic)
        {
            _cosmicScritchSource ??= new BitmapImage(
                new Uri(
                    "pack://application:,,,/Wobbi;component/Assets/Cosmic/wobbi-scritch.png",
                    UriKind.Absolute));

            CosmicImage.Source = _cosmicScritchSource;
        }


        // Reset any previous curious tilt.
        _curiousTilt.BeginAnimation(
            RotateTransform.AngleProperty, null);
        _curiousTilt.Angle = 0;

        // No swaying when sleeping or reduced motion is enabled.
        if (_isSleeping || _reducedAnimations)
            return;

        // Gentle side-to-side movement, like purring!
        var sway = new DoubleAnimation
        {
            From = -1.5,
            To = 1.5,
            Duration = TimeSpan.FromMilliseconds(750),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        _curiousTilt.BeginAnimation(
            RotateTransform.AngleProperty, sway);
    }


    private void StopHeadScritchies()
    {
        // Stop Wobbi's gentle swaying.
        _curiousTilt.BeginAnimation(
            RotateTransform.AngleProperty, null);

        _curiousTilt.Angle = 0;
        _isHappy = false;

        if (_isSleeping)
        {
            // Stay asleep, but keep smiling a little longer!
            OpenEyes.Visibility = Visibility.Collapsed;
            ClosedEyes.Visibility = Visibility.Collapsed;
            HappyEyes.Visibility = Visibility.Collapsed;
            SleepEyes.Visibility = Visibility.Collapsed;

            ScritchEyes.Visibility = Visibility.Visible;

            _sleepyScritchTimer.Stop();
            _sleepyScritchTimer.Start();
        }
        else
        {
            // Awake Wobbi returns to his normal face.
            ScritchEyes.Visibility = Visibility.Collapsed;
            ReturnToIdleFace();
        }
    }

    private void SleepyScritchTimer_Tick(
        object? sender,
        EventArgs e)
    {
        _sleepyScritchTimer.Stop();

        // Don't interrupt a new scritching session.
        if (_isHoldingPat)
            return;

        // Hide the contented smile.
        ScritchEyes.Visibility = Visibility.Collapsed;

        // Return to normal sleepy Wobbi.
        if (_isSleeping)
        {
            SleepEyes.Visibility = Visibility.Visible;

            if (_appearance == WobbiAppearance.Cosmic)
            {
                CosmicImage.Source = _cosmicBlinkSource;
            }

        }
    }




    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {

        if (_appearance == WobbiAppearance.Cosmic)
        {
            if (_isSleeping || _isHappy || _isHoldingPat)
                return;

            if (!_isBlinking)
            {
                _cosmicBlinkSource ??= new BitmapImage(
                    new Uri(
                        "pack://application:,,,/Wobbi;component/Assets/Cosmic/wobbi-blink.png",
                        UriKind.Absolute));

                CosmicImage.Source = _cosmicBlinkSource;

                _isBlinking = true;
                _blinkTimer.Interval =
                    TimeSpan.FromMilliseconds(140);
            }
            else
            {
                CosmicImage.Source = _cosmicIdleSource;

                _isBlinking = false;

                double nextBlink =
                    2.5 + _random.NextDouble() * 3;

                _blinkTimer.Interval =
                    TimeSpan.FromSeconds(nextBlink);
            }

            return;
        }

        if (_isSleeping || _isHappy)
        {
            return;
        }

        if (!_isBlinking)
        {
            OpenEyes.Visibility = Visibility.Collapsed;
            ClosedEyes.Visibility = Visibility.Visible;
            SleepEyes.Visibility = Visibility.Collapsed;

            _isBlinking = true;

            _blinkTimer.Interval = TimeSpan.FromMilliseconds(140);
        }
        else
        {
            ClosedEyes.Visibility = Visibility.Collapsed;
            OpenEyes.Visibility = Visibility.Visible;
            SleepEyes.Visibility = Visibility.Collapsed;

            _isBlinking = false;

            double nextBlink = 2.5 + _random.NextDouble() * 3;
            _blinkTimer.Interval = TimeSpan.FromSeconds(nextBlink);
        }

    }

    private void ToggleSleep()
    {
        _sleepyScritchTimer.Stop();
        ScritchEyes.Visibility = Visibility.Collapsed;
        _isSleeping = !_isSleeping;
        _happyResetTimer.Stop();
        StopHeartParticles();
        HappyEyes.Visibility = Visibility.Collapsed;
        _isHappy = false;

        if (_isSleeping)
        {
            _isBlinking = false;

            OpenEyes.Visibility = Visibility.Collapsed;
            ClosedEyes.Visibility = Visibility.Collapsed;
            SleepEyes.Visibility = Visibility.Visible;
        }
        else
        {
            SleepEyes.Visibility = Visibility.Collapsed;
            ClosedEyes.Visibility = Visibility.Collapsed;
            OpenEyes.Visibility = Visibility.Visible;

            _blinkTimer.Interval = TimeSpan.FromSeconds(2);
        }


        // Switch breathing speed
        UpdateBreathing();

        // Start or stop sleepy particles
        if (_isSleeping && !_reducedAnimations)
        {
            StartSleepParticles();
        }
        else
        {
            StopSleepParticles();
        }

        if (_appearance == WobbiAppearance.Cosmic)
        {
            // Reuse Cosmic's closed-eye blink artwork.
            _cosmicBlinkSource ??= new BitmapImage(
                new Uri(
                    "pack://application:,,,/Wobbi;component/Assets/Cosmic/wobbi-blink.png",
                    UriKind.Absolute));

            CosmicImage.Source = _isSleeping
                ? _cosmicBlinkSource
                : _cosmicIdleSource;

            // Reset the blink state on sleep/wake.
            _isBlinking = false;
        }

    }

    private void PlayAutoWakeHop()
    {
        if (_reducedAnimations)
            return;

        var hop = new DoubleAnimation
        {
            Duration = TimeSpan.FromMilliseconds(180),
            AutoReverse = true,
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        if (_isMiniMode)
        {
            // Hop the entire Mini Wobbi window.
            hop.From = Top;
            hop.To = Top - 12;

            BeginAnimation(Window.TopProperty, hop);
        }
        else
        {
            // Hop Full Wobbi using his existing transform.
            hop.From = 0;
            hop.To = -18;

            _petBounce.BeginAnimation(
                TranslateTransform.YProperty, hop);
        }
    }


    private void SleepWobbi_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Manual sleep/wake overrides Auto-Snooze.
        _isAutoSnoozing = false;

        ToggleSleep();
    }


    private void PetWobbi_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Wake Wobbi if he's sleeping
        if (_isSleeping)
        {
            ToggleSleep();
        }

        // Restart happy state cleanly
        _happyResetTimer.Stop();
        StopHeartParticles();

        ShowHappyFace();

        if (!_reducedAnimations)
        {
            StartHeartParticles();

            // Keep the familiar petting bounce in normal mode.
            var happyBounce = new DoubleAnimation
            {
                From = 0,
                To = -22,
                Duration = TimeSpan.FromMilliseconds(180),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new SineEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            _petBounce.BeginAnimation(
                TranslateTransform.YProperty,
                happyBounce
            );
        }

        // Return to idle after a short moment
        _happyResetTimer.Start();
    }

    private void Exit_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Stop Wobbi's active timers
        _blinkTimer.Stop();
        _happyResetTimer.Stop();
        _thinkingTimer.Stop();
        _replyHideTimer.Stop();

        // Close the whole application,
        // including the chat window
        Application.Current.Shutdown();
    }



    private void OpenChat_Click(
        object sender,
        RoutedEventArgs e)
    {

        // Opening chat clears Mini Wobbi's notification.
        MiniNotificationBadge.Visibility =
            Visibility.Collapsed;

        // Reuse the existing chat window

        if (_chatWindow != null)
        {
            // Reopen a chat hidden by the X button.
            if (!_chatWindow.IsVisible)
                _chatWindow.Show();

            if (_chatWindow.WindowState == WindowState.Minimized)
                _chatWindow.WindowState = WindowState.Normal;

            _chatWindow.Activate();
            return;
        }


        _chatWindow = new ChatWindow(
            ReactToMood,
            ShowSpeechBubble,
            SetThinkingBubble
        );

        _chatWindow.SetPersonality(_personality);


        // Keep the conversation alive when X is clicked.
        var chat = _chatWindow;

        chat.Closing += (_, e) =>
        {
            e.Cancel = true;
            chat.Hide();
        };

        _chatWindow.ReplyReceived += ChatWindow_ReplyReceived;

        // Clear notifications when the chat is brought into focus,
        // including when restored directly from the taskbar.
        _chatWindow.Activated += (_, _) =>
        {
            MiniNotificationBadge.Visibility =
                Visibility.Collapsed;
        };


        // Position chat beside Wobbi
        double spacing = 12;
        double rightSide = Left + ActualWidth + spacing;
        double leftSide = Left - _chatWindow.Width - spacing;

        // Keep within primary screen working area
        Rect screen = SystemParameters.WorkArea;

        double preferredLeft =
            rightSide + _chatWindow.Width <= screen.Right
            ? rightSide
            : leftSide;

        _chatWindow.Left = Math.Clamp(
            preferredLeft,
            screen.Left,
            Math.Max(screen.Left, screen.Right - _chatWindow.Width)
        );

        _chatWindow.Top = Math.Clamp(
            Top,
            screen.Top,
            Math.Max(screen.Top, screen.Bottom - _chatWindow.Height)
        );

        // Allow a new chat after closing this one
        _chatWindow.Closed += (_, _) =>
        {
            SetThinkingBubble(false);
            _chatWindow = null;
        };

        _chatWindow.Show();
    }


    private void ReactToMood(WobbiMood mood)
    {
        // No AI-triggered reactions while Wobbi is sleeping.
        if (_isSleeping)
            return;

        if (!_reducedAnimations)
        {
            WobbiAnimator.ReactToMood(
                mood,
                _petBounce,
                _curiousTilt,
                _isSleeping,
                _personality
            );
        }

        // A small smile belongs only to Happy / Excited reactions.
        // It returns to the neutral face after our existing timer.
        if (mood is WobbiMood.Happy or WobbiMood.Excited)
        {
            ShowHappyFace();
            _happyResetTimer.Stop();
            _happyResetTimer.Start();
        }
    }



    private void UpdateBreathing()
    {
        if (_reducedAnimations)
        {
            _breathing.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _breathing.ScaleY = 1.0;
            return;
        }

        WobbiAnimator.UpdateBreathing(
            _breathing,
            _isSleeping
        );
    }

    private void UpdateFloatingAnimation()
    {
        // Stop any existing float animation before changing motion settings.
        _floating.BeginAnimation(TranslateTransform.YProperty, null);
        _floating.Y = 0;

        if (_reducedAnimations)
            return;

        var floatAnimation = new DoubleAnimation
        {
            From = 0,
            To = -14,
            Duration = TimeSpan.FromSeconds(1.8),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        _floating.BeginAnimation(TranslateTransform.YProperty, floatAnimation);
    }


    private void StartSleepParticles()
    {
        WobbiAnimator.StartSleepParticles(
            SleepParticles,
            SleepZ1,
            SleepZ2,
            SleepZ3
        );
    }

    private void StopSleepParticles()
    {
        WobbiAnimator.StopSleepParticles(
            SleepParticles,
            SleepZ1,
            SleepZ2,
            SleepZ3
        );
    }


    private void StartHeartParticles()
    {
        WobbiAnimator.StartHeartParticles(
            HeartParticles,
            Heart1,
            Heart2,
            Heart3
        );
    }

    private void StopHeartParticles()
    {
        WobbiAnimator.StopHeartParticles(
            HeartParticles,
            Heart1,
            Heart2,
            Heart3
        );
    }


    private void ShowHappyFace()
    {
        _isHappy = true;

        if (_appearance == WobbiAppearance.Cosmic)
        {
            _cosmicHappySource ??= new BitmapImage(
                new Uri(
                    "pack://application:,,,/Wobbi;component/Assets/Cosmic/wobbi-happy.png",
                    UriKind.Absolute));

            CosmicImage.Source = _cosmicHappySource;
            return;
        }


        OpenEyes.Visibility = Visibility.Collapsed;
        ClosedEyes.Visibility = Visibility.Collapsed;
        SleepEyes.Visibility = Visibility.Collapsed;
        HappyEyes.Visibility = Visibility.Visible;
    }

    private void ReturnToIdleFace()
    {
        _isHappy = false;

        if (_appearance == WobbiAppearance.Cosmic)
        {
            CosmicImage.Source = _cosmicIdleSource;
            return;
        }


        HappyEyes.Visibility = Visibility.Collapsed;
        ClosedEyes.Visibility = Visibility.Collapsed;
        SleepEyes.Visibility = Visibility.Collapsed;
        OpenEyes.Visibility = Visibility.Visible;
    }

    private void HappyResetTimer_Tick(object? sender, EventArgs e)
    {
        _happyResetTimer.Stop();
        StopHeartParticles();
        ReturnToIdleFace();
    }

    // Show an animated typing bubble above Wobbi while Ollama is responding.
    // This reuses SpeechBubble and SpeechBubbleText from MainWindow.xaml.
    private void SetThinkingBubble(bool thinking)
    {
        if (thinking && (_isMiniMode || !_showSpeechBubbles))
            return;

        if (thinking)
        {
            _replyHideTimer.Stop();
            _thinkingTimer.Stop();
            _isThinking = true;
            _thinkingDotCount = 1;

            // Cancel any previous reply fade-out before showing the dots.
            SpeechBubble.BeginAnimation(UIElement.OpacityProperty, null);
            SpeechBubbleText.Text = ".";
            SpeechBubble.Opacity = 1;
            SpeechBubble.Visibility = Visibility.Visible;
            _thinkingTimer.Start();
        }
        else
        {
            // Important: a second stop (in finally) must not hide the reply!
            if (!_isThinking)
                return;

            _isThinking = false;
            _thinkingTimer.Stop();
            SpeechBubble.BeginAnimation(UIElement.OpacityProperty, null);
            SpeechBubble.Visibility = Visibility.Collapsed;
            SpeechBubbleText.Text = string.Empty;
        }
    }

    private void ThinkingTimer_Tick(object? sender, EventArgs e)
    {
        _thinkingDotCount = (_thinkingDotCount % 3) + 1;
        SpeechBubbleText.Text = new string('.', _thinkingDotCount);
    }

    private void ReplyHideTimer_Tick(object? sender, EventArgs e)
    {
        _replyHideTimer.Stop();
        if (_isThinking)
            return;

        SpeechBubble.Visibility = Visibility.Collapsed;
        SpeechBubble.Opacity = 0;
    }

    private void HideSpeechBubble()
    {
        _replyHideTimer.Stop();
        _thinkingTimer.Stop();
        _isThinking = false;
        SpeechBubble.BeginAnimation(UIElement.OpacityProperty, null);
        SpeechBubble.Visibility = Visibility.Collapsed;
        SpeechBubble.Opacity = 0;
        SpeechBubbleText.Text = string.Empty;
    }

    private void ShowSpeechBubble(string message)
    {
        if (_isMiniMode || !_showSpeechBubbles)
            return;

        _replyHideTimer.Stop();

        // Show a short preview of Wobbi's reply
        const int maxLength = 100;

        string preview = message.Length > maxLength
            ? message[..97].TrimEnd() + "..."
            : message;

        SpeechBubbleText.Text = preview;

        SpeechBubble.Visibility = Visibility.Visible;
        SpeechBubble.BeginAnimation(UIElement.OpacityProperty, null);

        if (_reducedAnimations)
        {
            // No fading; still dismiss the bubble after five seconds.
            SpeechBubble.Opacity = 1;
            _replyHideTimer.Start();
            return;
        }

        SpeechBubble.Opacity = 0;

        // Create a gentle fade-in and fade-out
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(5),
            FillBehavior = FillBehavior.Stop
        };

        // Start invisible
        animation.KeyFrames.Add(
            new DiscreteDoubleKeyFrame(
                0,
                KeyTime.FromTimeSpan(TimeSpan.Zero)
            )
        );

        // Fade in
        animation.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(0.25))
            )
        );

        // Stay visible
        animation.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(4))
            )
        );

        // Fade out
        animation.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                0,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(5))
            )
        );

        SpeechBubble.BeginAnimation(
            UIElement.OpacityProperty,
            animation
        );
    }

    private void ClassicAppearance_Click(object sender, RoutedEventArgs e)
    => SetAppearance(WobbiAppearance.Classic);

    private void LavenderAppearance_Click(object sender, RoutedEventArgs e)
        => SetAppearance(WobbiAppearance.Lavender);


    private void CosmicAppearance_Click(
    object sender, RoutedEventArgs e)
    => SetAppearance(WobbiAppearance.Cosmic);


    private void SetAppearance(WobbiAppearance appearance)
    {
        if (_appearance == appearance)
        {
            ApplyAppearance();
            return;
        }

        _appearance = appearance;
        ApplyAppearance();
        _appearanceSettings.Save(appearance);
    }


    private void ApplyAppearance()
    {
        bool isCosmic =
            _appearance == WobbiAppearance.Cosmic;

        // Update appearance menu checkmarks.
        ClassicAppearanceMenuItem.IsChecked =
            _appearance == WobbiAppearance.Classic;

        LavenderAppearanceMenuItem.IsChecked =
            _appearance == WobbiAppearance.Lavender;

        CosmicAppearanceMenuItem.IsChecked = isCosmic;

        // Cosmic currently uses its approved idle artwork.
        CosmicImage.Visibility = isCosmic
            ? Visibility.Visible
            : Visibility.Collapsed;


        // Hide the original SVG artwork whenever Cosmic is selected.
        // Opacity stays at 0 even when animation timers
        // change the SVGs' Visibility properties.

        double originalOpacity = isCosmic ? 0 : 1;

        OpenEyes.Opacity = originalOpacity;
        ClosedEyes.Opacity = originalOpacity;
        SleepEyes.Opacity = originalOpacity;
        HappyEyes.Opacity = originalOpacity;
        ScritchEyes.Opacity = originalOpacity;


        if (isCosmic)
        {
            // Keep Cosmic's approved idle artwork ready.
            _cosmicIdleSource ??= CosmicImage.Source;
            CosmicImage.Source = _cosmicIdleSource;

            _isBlinking = false;
            _blinkTimer.Interval = TimeSpan.FromSeconds(3);
        }


        if (isCosmic)
        {
            // Hide all Classic/Lavender expressions.
            OpenEyes.Visibility = Visibility.Collapsed;
            ClosedEyes.Visibility = Visibility.Collapsed;
            SleepEyes.Visibility = Visibility.Collapsed;
            HappyEyes.Visibility = Visibility.Collapsed;
            ScritchEyes.Visibility = Visibility.Collapsed;

            // Show our approved Cosmic artwork.
            CosmicImage.Visibility = Visibility.Visible;
            return;
        }

        // Hide Cosmic when switching back.
        CosmicImage.Visibility = Visibility.Collapsed;

        // Restore the correct Classic/Lavender expression.
        OpenEyes.Visibility = Visibility.Collapsed;
        ClosedEyes.Visibility = Visibility.Collapsed;
        SleepEyes.Visibility = Visibility.Collapsed;
        HappyEyes.Visibility = Visibility.Collapsed;
        ScritchEyes.Visibility = Visibility.Collapsed;

        if (_isSleeping)
            SleepEyes.Visibility = Visibility.Visible;
        else if (_isHoldingPat)
            ScritchEyes.Visibility = Visibility.Visible;
        else if (_isHappy)
            HappyEyes.Visibility = Visibility.Visible;
        else
            OpenEyes.Visibility = Visibility.Visible;


        // Classic and Lavender keep their existing SVGs.
        string folder =
            _appearance == WobbiAppearance.Lavender
            ? "Assets/Lavender/"
            : "Assets/";

        string baseUri =
            $"pack://application:,,,/Wobbi;component/{folder}";

        OpenEyes.Source = new Uri(
            baseUri + "wobbi.svg",
            UriKind.Absolute);

        ClosedEyes.Source = new Uri(
            baseUri + "wobbi-blink.svg",
            UriKind.Absolute);

        SleepEyes.Source = new Uri(
            baseUri + "wobbi-sleep.svg",
            UriKind.Absolute);

        HappyEyes.Source = new Uri(
            baseUri + "wobbi-happy.svg",
            UriKind.Absolute);

        ScritchEyes.Source = new Uri(
            baseUri + "wobbi-scritch.svg",
            UriKind.Absolute);
    }


    private void CalmPersonality_Click(object sender, RoutedEventArgs e)
    => SetPersonality(WobbiPersonality.Calm);

    private void BalancedPersonality_Click(object sender, RoutedEventArgs e)
        => SetPersonality(WobbiPersonality.Balanced);

    private void PlayfulPersonality_Click(object sender, RoutedEventArgs e)
        => SetPersonality(WobbiPersonality.Playful);

    private void SetPersonality(WobbiPersonality personality)
    {
        _personality = personality;
        _appearanceSettings.SavePersonality(personality);
        ApplyPersonalityMenu();
        _chatWindow?.SetPersonality(personality);
    }

    private void ApplyPersonalityMenu()
    {
        CalmPersonalityMenuItem.IsChecked = _personality == WobbiPersonality.Calm;
        BalancedPersonalityMenuItem.IsChecked = _personality == WobbiPersonality.Balanced;
        PlayfulPersonalityMenuItem.IsChecked = _personality == WobbiPersonality.Playful;
    }

    private void ShowSpeechBubbles_Click(object sender, RoutedEventArgs e)
    {
        _showSpeechBubbles = !_showSpeechBubbles;
        _appearanceSettings.SaveShowSpeechBubbles(_showSpeechBubbles);
        ApplyPreferencesMenu();

        if (!_showSpeechBubbles)
            HideSpeechBubble();
    }

    private void ReducedAnimations_Click(object sender, RoutedEventArgs e)
    {
        _reducedAnimations = !_reducedAnimations;
        _appearanceSettings.SaveReducedAnimations(_reducedAnimations);
        ApplyPreferencesMenu();

        UpdateFloatingAnimation();
        UpdateBreathing();

        if (_reducedAnimations)
        {
            // Stop an in-progress bounce/tilt as soon as reduced motion is selected.
            _petBounce.BeginAnimation(TranslateTransform.YProperty, null);
            _petBounce.Y = 0;
            _curiousTilt.BeginAnimation(RotateTransform.AngleProperty, null);
            _curiousTilt.Angle = 0;
            StopHeartParticles();
            StopSleepParticles();
        }
        else if (_isSleeping)
        {
            StartSleepParticles();
        }
    }

    private void AutoSnooze_Click(
        object sender,
        RoutedEventArgs e)
    {
        _autoSnoozeEnabled = AutoSnoozeMenuItem.IsChecked;

        _appearanceSettings.SaveAutoSnooze(
            _autoSnoozeEnabled
        );
    }



    private void ApplyPreferencesMenu()
    {
        ShowSpeechBubblesMenuItem.IsChecked = _showSpeechBubbles;
        ReducedAnimationsMenuItem.IsChecked = _reducedAnimations;
        AutoSnoozeMenuItem.IsChecked = _autoSnoozeEnabled;
    }



    private void ToggleMiniMode_Click(
        object sender,
        RoutedEventArgs e)
    {
        _isMiniMode = !_isMiniMode;
        ApplyMiniMode();

        // Remember Wobbi's selected size.
        _appearanceSettings.SaveMiniMode(_isMiniMode);
    }


    private void ApplyMiniMode()
    {
        if (_isMiniMode)
        {
            // Clear a lingering smile before minimizing.
            _sleepyScritchTimer.Stop();
            ScritchEyes.Visibility = Visibility.Collapsed;

            if (_isSleeping)
                SleepEyes.Visibility = Visibility.Visible;
            // Remember Full Wobbi's position.
            _fullModeLeft = Left;
            _fullModeTop = Top;
            SetMiniFace("mini-idle.png");

            // Always use the original Mini Wobbi,
            // regardless of the selected Full Mode appearance.
            MiniFrameTestImage.Visibility = Visibility.Visible;
            MiniBlinkImage.Visibility = Visibility.Collapsed;
            _miniBlinking = false;

            _miniLookingAround = false;
            _miniGlanceTimer.Interval = TimeSpan.FromSeconds(8);

            // Hide Full Wobbi and his speech bubble.
            HideSpeechBubble();
            WobbiImage.Visibility = Visibility.Collapsed;

            // Show Mini Wobbi.
            MiniView.Visibility = Visibility.Visible;

            Width = 76;
            Height = 72;

            // Find the usable desktop area.
            Rect workArea = SystemParameters.WorkArea;

            // Leave a little space around Mini Wobbi.
            const double margin = 16;

            // Park him in the bottom-right corner.
            Left = Math.Max(
                workArea.Left,
                workArea.Right - Width - margin
            );

            Top = Math.Max(
                workArea.Top,
                workArea.Bottom - Height - margin
            );

            MiniModeMenuItem.Header = "Switch to Full Mode";
        }
        else
        {
            // Hide Mini Wobbi.
            MiniView.Visibility = Visibility.Collapsed;

            // Restore the original window size.

            Width = 260;
            Height = 260;


            // Return Full Wobbi to his previous position!
            Left = _fullModeLeft;
            Top = _fullModeTop;

            WobbiImage.Visibility = Visibility.Visible;

            MiniModeMenuItem.Header = "Switch to Mini Mode";
        }
    }


    private void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        // Find the usable screen area, excluding the taskbar.
        Rect workArea = SystemParameters.WorkArea;

        const double margin = 16;

        // Start Full Wobbi in the bottom-right corner.
        Left = Math.Max(
            workArea.Left,
            workArea.Right - ActualWidth - margin
        );

        Top = Math.Max(
            workArea.Top,
            workArea.Bottom - ActualHeight - margin
        );


        // Restore Mini Mode if it was selected last time.
        if (_appearanceSettings.LoadMiniMode())
        {
            _isMiniMode = true;
            ApplyMiniMode();
        }

        // Restore unread agenda notifications after restart.
        MiniAgendaBadge.Visibility =
            _agendaService.HasPendingReminders()
                ? Visibility.Visible
                : Visibility.Collapsed;


        // Restore the unread badge.
        RefreshReminderBadge();


        if (_agendaService.RecoveredFromBackup)
        {
            MessageBox.Show(
                this,
                "Wobbi noticed your agenda file was damaged! 💜\n\n" +
                "I've restored the previous saved version.\n\n" +
                "The backup may be one save older, so please " +
                "check your appointments.\n\n" +
                "The damaged original has been preserved.",
                "Wobbi Agenda Recovery",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }


    }

    private void MiniClickTimer_Tick(
        object? sender,
        EventArgs e)
    {
        _miniClickTimer.Stop();

        if (!_isMiniMode)
            return;

        // Open or focus Wobbi's existing chat.
        OpenChat_Click(this, new RoutedEventArgs());
    }

    private void ChatWindow_ReplyReceived(
        object? sender,
        EventArgs e)
    {
        // Notifications only appear in Mini Mode.
        if (!_isMiniMode || _chatWindow == null)
            return;

        // No notification needed if you're already
        // looking at Wobbi's chat.
        if (_chatWindow.IsActive &&
            _chatWindow.WindowState != WindowState.Minimized)
        {
            return;
        }

        // A new unread AI reply!
        MiniNotificationBadge.Visibility =
            Visibility.Visible;
    }

    private void MiniBlinkTimer_Tick(
        object? sender,
        EventArgs e)

    {

        // Only animate when Mini Wobbi is awake.
        if (!_isMiniMode || _isSleeping)
        {
            ResetMiniBlink();
            _miniBlinkTimer.Interval =
                TimeSpan.FromSeconds(4);
            return;
        }

        if (_miniBlinking)
        {
            // Open his eyes again.
            ResetMiniBlink();

            // Wait 3-6 seconds before the next blink.
            double nextBlink =
                3 + _random.NextDouble() * 3;

            _miniBlinkTimer.Interval =
                TimeSpan.FromSeconds(nextBlink);
        }

        else
        {
            // Keep his normal face visible underneath.
            MiniFrameTestImage.Visibility =
                Visibility.Visible;

            // Display the blinking face on top.
            MiniBlinkImage.Visibility =
                Visibility.Visible;

            _miniBlinking = true;

            _miniBlinkTimer.Interval =
                TimeSpan.FromMilliseconds(140);
        }

    }

    private void ResetMiniBlink()
    {
        _miniBlinking = false;

        MiniBlinkImage.Visibility =
            Visibility.Collapsed;

        MiniFrameTestImage.Visibility =
            Visibility.Visible;
    }

    private void MiniGlanceTimer_Tick(
        object? sender,
        EventArgs e)
    {
        // Only glance when Mini Wobbi is awake
        // and reduced animations are disabled.
        if (!_isMiniMode || _isSleeping || _reducedAnimations)
        {
            SetMiniFace("mini-idle.png");
            _miniLookingAround = false;
            _miniGlanceTimer.Interval = TimeSpan.FromSeconds(8);
            return;
        }

        if (_miniLookingAround)
        {
            // Return his eyes to the center.
            SetMiniFace("mini-idle.png");
            _miniLookingAround = false;

            double wait = _personality switch
            {
                WobbiPersonality.Calm => 12 + _random.NextDouble() * 5,
                WobbiPersonality.Playful => 5 + _random.NextDouble() * 4,
                _ => 8 + _random.NextDouble() * 4
            };

            _miniGlanceTimer.Interval = TimeSpan.FromSeconds(wait);
        }
        else
        {
            // Randomly look left or right.
            string image = _random.Next(2) == 0
                ? "mini-look-left.png"
                : "mini-look-right.png";

            SetMiniFace(image);
            _miniLookingAround = true;

            // Hold the glance for a moment.
            _miniGlanceTimer.Interval =
                TimeSpan.FromMilliseconds(650);
        }
    }

    private void SetMiniFace(string filename)
    {
        MiniFrameTestImage.Source = new BitmapImage(
            new Uri(
                $"pack://application:,,,/Wobbi;component/Assets/Mini/{filename}",
                UriKind.Absolute
            )
        );
    }


    private void PlayScritchReleaseBounce()
    {
        // A tiny, contented hop after scritchies.
        var bounce = new DoubleAnimation
        {
            From = 0,
            To = -8,
            Duration = TimeSpan.FromMilliseconds(170),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1),
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        _petBounce.BeginAnimation(
            TranslateTransform.YProperty,
            bounce
        );
    }



    private void OpenAgenda_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Opening Agenda does NOT dismiss reminders.
        RefreshReminderBadge();

        // Focus the existing Agenda window.
        if (_agendaWindow != null)
        {
            if (_agendaWindow.WindowState ==
                WindowState.Minimized)
            {
                _agendaWindow.WindowState =
                    WindowState.Normal;
            }

            _agendaWindow.Activate();
            return;
        }

        // Otherwise, create the Agenda window.

        var agenda = new AgendaWindow(_agendaService)
        {
            Topmost = false,
            ShowInTaskbar = true
        };

        _agendaWindow = agenda;

        agenda.Closed += (_, _) =>
        {
            _agendaWindow = null;
        };

        // Open independently from Wobbi's always-on-top window.
        agenda.Show();
        agenda.Activate();

    }



    private void ShowAgendaReminder(AgendaEvent agendaEvent)
    {
        var localTime = agendaEvent.StartsAt.ToLocalTime();

        string message =
            $"Pssst! 💜 {agendaEvent.Title} " +
            $"at {localTime:HH:mm}! " +
            "Don't forget, habibs! 🐣";

        // Keep the persistent reminder badge updated.
        RefreshReminderBadge();

        // Refresh the Reminder Center if already open.
        // Do not activate or open it automatically.
        _reminderCenter?.Refresh();

        // Close an older toast if one is still visible.
        _reminderToast?.Close();

        // Show a small, non-activating notification.
        var toast = new ReminderToastWindow(agendaEvent);

        _reminderToast = toast;

        toast.Closed += (_, _) =>
        {
            if (ReferenceEquals(_reminderToast, toast))
                _reminderToast = null;
        };

        toast.Show();

        // Preserve Wobbi's existing speech bubble setting.
        if (!_isMiniMode && _showSpeechBubbles)
        {
            ShowSpeechBubble(message);
        }
    }

}