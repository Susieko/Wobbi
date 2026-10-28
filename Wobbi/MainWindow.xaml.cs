
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
namespace Wobbi;

public partial class MainWindow : Window
{

    private readonly DispatcherTimer _blinkTimer = new();
    private readonly Random _random = new();
    private readonly TranslateTransform _floating = new();
    private readonly TranslateTransform _petBounce = new();
    private readonly RotateTransform _curiousTilt = new();
    private bool _isBlinking = false;
    private bool _isSleeping = false;
    private bool _isMiniMode = false;
    private double _fullModeLeft;
    private double _fullModeTop;


    private void Window_MouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click switches between Full and Mini Mode.
            ToggleMiniMode_Click(sender, e);

            e.Handled = true;
            return;
        }

        // Single-click and drag still moves Wobbi.
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
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

    public MainWindow()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.Manual;
        Loaded += MainWindow_Loaded;
        // Read preferences before starting Wobbi's idle animations.
        _showSpeechBubbles = _appearanceSettings.LoadShowSpeechBubbles();
        _reducedAnimations = _appearanceSettings.LoadReducedAnimations();

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

        _appearance = _appearanceSettings.Load();
        ApplyAppearance();
        _personality = _appearanceSettings.LoadPersonality();
        ApplyPersonalityMenu();
        ApplyPreferencesMenu();
    }

    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {
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
    }


    private void SleepWobbi_Click(
        object sender,
        RoutedEventArgs e)
    {
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
        // Reuse the existing chat window
        if (_chatWindow != null)
        {
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

        OpenEyes.Visibility = Visibility.Collapsed;
        ClosedEyes.Visibility = Visibility.Collapsed;
        SleepEyes.Visibility = Visibility.Collapsed;
        HappyEyes.Visibility = Visibility.Visible;
    }

    private void ReturnToIdleFace()
    {
        _isHappy = false;

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
        // Classic retains the ORIGINAL files; no need to move or rename them.
        string folder = _appearance == WobbiAppearance.Lavender
            ? "Assets/Lavender/"
            : "Assets/";

        OpenEyes.Source = new Uri($"pack://application:,,,/{folder}wobbi.svg",
                                  UriKind.Absolute);
        ClosedEyes.Source = new Uri($"pack://application:,,,/{folder}wobbi-blink.svg",
                                    UriKind.Absolute);
        SleepEyes.Source = new Uri($"pack://application:,,,/{folder}wobbi-sleep.svg",
                                   UriKind.Absolute);
        HappyEyes.Source = new Uri($"pack://application:,,,/{folder}wobbi-happy.svg",
                                   UriKind.Absolute);

        ClassicAppearanceMenuItem.IsChecked = _appearance == WobbiAppearance.Classic;
        LavenderAppearanceMenuItem.IsChecked = _appearance == WobbiAppearance.Lavender;
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

    private void ApplyPreferencesMenu()
    {
        ShowSpeechBubblesMenuItem.IsChecked = _showSpeechBubbles;
        ReducedAnimationsMenuItem.IsChecked = _reducedAnimations;
    }


    private void ToggleMiniMode_Click(
        object sender,
        RoutedEventArgs e)
    {
        _isMiniMode = !_isMiniMode;
        ApplyMiniMode();
    }


    private void ApplyMiniMode()
    {
        if (_isMiniMode)
        {
            // Remember Full Wobbi's position.
            _fullModeLeft = Left;
            _fullModeTop = Top;

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
            Width = 300;
            Height = 300;

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
    }


}