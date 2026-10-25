
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
    private readonly TranslateTransform _petBounce = new();
    private readonly RotateTransform _curiousTilt = new();
    private bool _isBlinking = false;
    private bool _isSleeping = false;
    private ChatWindow? _chatWindow;
    private readonly ScaleTransform _breathing = new(1, 1);
    private readonly DispatcherTimer _happyResetTimer = new();
    private bool _isHappy = false;

    public MainWindow()
    {
        InitializeComponent();

        var floating = new TranslateTransform();

        var transforms = new TransformGroup();
        transforms.Children.Add(_breathing);
        transforms.Children.Add(floating);
        transforms.Children.Add(_petBounce);
        transforms.Children.Add(_curiousTilt);

        WobbiImage.RenderTransform = transforms;
        WobbiImage.RenderTransformOrigin = new Point(0.5, 0.85);



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

        floating.BeginAnimation(
            TranslateTransform.YProperty,
            floatAnimation
        );

        UpdateBreathing();

        _blinkTimer.Interval = TimeSpan.FromSeconds(3);
        _blinkTimer.Tick += BlinkTimer_Tick;
        _blinkTimer.Start();
        _happyResetTimer.Interval = TimeSpan.FromSeconds(2.0);
        _happyResetTimer.Tick += HappyResetTimer_Tick;
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
        if (_isSleeping)
        {
            StartSleepParticles();
        }
        else
        {
            StopSleepParticles();
        }

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
        StartHeartParticles();

        // Happy bounce
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

        // Return to idle after a short moment
        _happyResetTimer.Start();
    }


    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleSleep();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }


    private void Exit_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Stop Wobbi's active timers
        _blinkTimer.Stop();
        _happyResetTimer.Stop();

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
            ShowSpeechBubble
        );

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
            _chatWindow = null;
        };

        _chatWindow.Show();
    }


    private void ReactToMood(WobbiMood mood)
    {
        WobbiAnimator.ReactToMood(
            mood,
            _petBounce,
            _curiousTilt,
            _isSleeping
        );
    }



    private void UpdateBreathing()
    {
        WobbiAnimator.UpdateBreathing(
            _breathing,
            _isSleeping
        );
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

    private void ShowSpeechBubble(string message)
    {
        // Show a short preview of Wobbi's reply
        const int maxLength = 100;

        string preview = message.Length > maxLength
            ? message[..97].TrimEnd() + "..."
            : message;

        SpeechBubbleText.Text = preview;

        SpeechBubble.Visibility = Visibility.Visible;
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

}