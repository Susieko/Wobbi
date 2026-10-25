
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wobbi.Services;
using System.Windows.Controls;
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
        _happyResetTimer.Interval = TimeSpan.FromSeconds(1.4);
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
        _blinkTimer.Stop();
        Close();
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
        // Respect Wobbi's sleep mode
        if (_isSleeping)
            return;


        if (mood == WobbiMood.Curious)
        {
            var tilt = new DoubleAnimation
            {
                From = -4,
                To = 4,
                Duration = TimeSpan.FromMilliseconds(180),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),

                // Return to the original rotation afterward!
                FillBehavior = FillBehavior.Stop,

                EasingFunction = new SineEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            _curiousTilt.BeginAnimation(
                RotateTransform.AngleProperty,
                tilt
            );

            return;
        }

        if (mood == WobbiMood.Neutral)
            return;

        // Excited = higher and more hops
        bool excited = mood == WobbiMood.Excited;

        var bounce = new DoubleAnimation
        {
            From = 0,
            To = excited ? -28 : -16,
            Duration = TimeSpan.FromMilliseconds(180),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(
                excited ? 2 : 1
            ),
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

    private void UpdateBreathing()
    {
        // Sleeping = slower and deeper breathing
        double minScale = _isSleeping ? 0.98 : 1.0;
        double maxScale = _isSleeping ? 1.075 : 1.04;

        double seconds = _isSleeping ? 2.4 : 1.3;

        var animation = new DoubleAnimation
        {
            From = minScale,
            To = maxScale,

            Duration = TimeSpan.FromSeconds(seconds),

            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,

            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        _breathing.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            animation
        );
    }


    private void StartSleepParticles()
    {
        SleepParticles.Visibility = Visibility.Visible;

        // Stagger the particles so they don't move together
        AnimateSleepZ(SleepZ1, 0);
        AnimateSleepZ(SleepZ2, 0.8);
        AnimateSleepZ(SleepZ3, 1.6);
    }

    private static void AnimateSleepZ(
        TextBlock z,
        double delay)
    {
        var duration = TimeSpan.FromSeconds(2.6);
        var startDelay = TimeSpan.FromSeconds(delay);

        // Each Z grows a little as it floats
        var scale = new ScaleTransform(0.65, 0.65);
        var movement = new TranslateTransform();

        var transforms = new TransformGroup();
        transforms.Children.Add(scale);
        transforms.Children.Add(movement);

        z.RenderTransform = transforms;
        z.RenderTransformOrigin = new Point(0.5, 0.5);

        // Float upward
        var floatUp = new DoubleAnimation
        {
            From = 15,
            To = -90,
            Duration = duration,
            BeginTime = startDelay,
            RepeatBehavior = RepeatBehavior.Forever
        };

        movement.BeginAnimation(
            TranslateTransform.YProperty,
            floatUp
        );

        // Grow while floating
        var grow = new DoubleAnimation
        {
            From = 0.65,
            To = 1.3,
            Duration = duration,
            BeginTime = startDelay,
            RepeatBehavior = RepeatBehavior.Forever
        };

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty, grow);

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty, grow);

        // Fade in, float, then disappear
        var fade = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration,
            BeginTime = startDelay,
            RepeatBehavior = RepeatBehavior.Forever
        };

        fade.KeyFrames.Add(
            new DiscreteDoubleKeyFrame(
                0, KeyTime.FromTimeSpan(TimeSpan.Zero)));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1, KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(0.4))));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1, KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(1.6))));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                0, KeyTime.FromTimeSpan(duration)));

        z.BeginAnimation(
            UIElement.OpacityProperty, fade);
    }

    private void StopSleepParticles()
    {
        SleepParticles.Visibility = Visibility.Collapsed;

        foreach (TextBlock z in new[]
        {
        SleepZ1, SleepZ2, SleepZ3
    })
        {
            z.BeginAnimation(
                UIElement.OpacityProperty, null);

            z.Opacity = 0;
            z.RenderTransform = Transform.Identity;
        }
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

    private void StartHeartParticles()
    {
        HeartParticles.Visibility = Visibility.Visible;

        AnimateHeart(Heart1, 0.0, -18);
        AnimateHeart(Heart2, 0.25, 0);
        AnimateHeart(Heart3, 0.5, 18);
    }

    private static void AnimateHeart(
        TextBlock heart,
        double delay,
        double driftX)
    {
        var duration = TimeSpan.FromSeconds(1.4);
        var startDelay = TimeSpan.FromSeconds(delay);

        var scale = new ScaleTransform(0.7, 0.7);
        var movement = new TranslateTransform();

        var transforms = new TransformGroup();
        transforms.Children.Add(scale);
        transforms.Children.Add(movement);

        heart.RenderTransform = transforms;
        heart.RenderTransformOrigin = new Point(0.5, 0.5);

        var floatUp = new DoubleAnimation
        {
            From = 8,
            To = -95,
            Duration = duration,
            BeginTime = startDelay,
            FillBehavior = FillBehavior.Stop
        };

        movement.BeginAnimation(
            TranslateTransform.YProperty,
            floatUp
        );

        var drift = new DoubleAnimation
        {
            From = 0,
            To = driftX,
            Duration = duration,
            BeginTime = startDelay,
            FillBehavior = FillBehavior.Stop
        };

        movement.BeginAnimation(
            TranslateTransform.XProperty,
            drift
        );

        var grow = new DoubleAnimation
        {
            From = 0.7,
            To = 1.15,
            Duration = duration,
            BeginTime = startDelay,
            FillBehavior = FillBehavior.Stop
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);

        var fade = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration,
            BeginTime = startDelay
        };

        fade.KeyFrames.Add(
            new DiscreteDoubleKeyFrame(
                0, KeyTime.FromTimeSpan(TimeSpan.Zero)));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.18))));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.85))));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                0, KeyTime.FromTimeSpan(duration)));

        heart.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void StopHeartParticles()
    {
        HeartParticles.Visibility = Visibility.Collapsed;

        foreach (TextBlock heart in new[] { Heart1, Heart2, Heart3 })
        {
            heart.BeginAnimation(UIElement.OpacityProperty, null);
            heart.Opacity = 0;
            heart.RenderTransform = Transform.Identity;
        }
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