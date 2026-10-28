
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wobbi.Models;


namespace Wobbi.Animations;

public static class WobbiAnimator
{
    public static void ReactToMood(
        WobbiMood mood,
        TranslateTransform bounce,
        RotateTransform tilt,
        bool isSleeping,
        WobbiPersonality personality)
    {
        // AI-driven reactions should never interrupt sleep.
        if (isSleeping)
            return;

        switch (mood)
        {
            case WobbiMood.Happy:
                double happyHeight = personality switch
                {
                    WobbiPersonality.Calm => 6,
                    WobbiPersonality.Balanced => 16,
                    WobbiPersonality.Playful => 22,
                    _ => 16
                };
                int happyHops = personality == WobbiPersonality.Playful ? 2 : 1;
                PlayBounce(bounce, happyHeight, happyHops);
                break;

            case WobbiMood.Excited:
                double excitedHeight = personality switch
                {
                    WobbiPersonality.Calm => 12,
                    WobbiPersonality.Balanced => 28,
                    WobbiPersonality.Playful => 35,
                    _ => 28
                };
                int excitedHops = personality switch
                {
                    WobbiPersonality.Calm => 1,
                    WobbiPersonality.Balanced => 2,
                    WobbiPersonality.Playful => 3,
                    _ => 2
                };
                PlayBounce(bounce, excitedHeight, excitedHops);
                break;

            case WobbiMood.Curious:
                double angle = personality switch
                {
                    WobbiPersonality.Calm => 2,
                    WobbiPersonality.Balanced => 4,
                    WobbiPersonality.Playful => 6,
                    _ => 4
                };
                PlayCuriousTilt(tilt, angle);
                break;

            case WobbiMood.Neutral:
            default:
                // Neutral Wobbi keeps floating and breathing.
                break;
        }
    }

    // Wobbi tilts when curious

    private static void PlayCuriousTilt(
        RotateTransform tilt,
        double angle)
    {
        var animation = new DoubleAnimation
        {
            From = -angle,
            To = angle,

            Duration = TimeSpan.FromMilliseconds(180),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2),
            FillBehavior = FillBehavior.Stop,

            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        tilt.BeginAnimation(
            RotateTransform.AngleProperty,
            animation
        );
    }


    // Wobbi bounces when happy or excited
    private static void PlayBounce(
            TranslateTransform bounce,
            double height,
            int repetitions)
    {
        var animation = new DoubleAnimation
        {
            From = 0,
            To = -height,

            Duration =
                TimeSpan.FromMilliseconds(180),

            AutoReverse = true,

            RepeatBehavior =
                new RepeatBehavior(repetitions),

            FillBehavior = FillBehavior.Stop,

            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        bounce.BeginAnimation(
            TranslateTransform.YProperty,
            animation
        );
    }

    public static void UpdateBreathing(
        ScaleTransform breathing,
        bool isSleeping)
    {
        // Sleeping Wobbi breathes slower and deeper
        double minScale = isSleeping ? 0.98 : 1.0;
        double maxScale = isSleeping ? 1.075 : 1.04;

        double duration = isSleeping ? 2.4 : 1.3;

        var animation = new DoubleAnimation
        {
            From = minScale,
            To = maxScale,

            Duration = TimeSpan.FromSeconds(duration),

            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,

            EasingFunction = new SineEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        breathing.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            animation
        );
    }


    public static void StartSleepParticles(
        UIElement particles,
        TextBlock z1,
        TextBlock z2,
        TextBlock z3)
    {
        particles.Visibility = Visibility.Visible;

        // Each Z appears at a different moment
        AnimateSleepZ(z1, 0);
        AnimateSleepZ(z2, 0.8);
        AnimateSleepZ(z3, 1.6);
    }

    private static void AnimateSleepZ(
        TextBlock z,
        double delay)
    {
        var duration = TimeSpan.FromSeconds(2.6);
        var startDelay = TimeSpan.FromSeconds(delay);

        // Create movement and scaling
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

        // Grow slightly
        var grow = new DoubleAnimation
        {
            From = 0.65,
            To = 1.3,
            Duration = duration,
            BeginTime = startDelay,
            RepeatBehavior = RepeatBehavior.Forever
        };

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            grow
        );

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            grow
        );

        // Fade in and disappear
        var fade = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration,
            BeginTime = startDelay,
            RepeatBehavior = RepeatBehavior.Forever
        };

        fade.KeyFrames.Add(
            new DiscreteDoubleKeyFrame(
                0,
                KeyTime.FromTimeSpan(TimeSpan.Zero)
            )
        );

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(0.4))
            )
        );

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(1.6))
            )
        );

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                0,
                KeyTime.FromTimeSpan(duration)
            )
        );

        z.BeginAnimation(
            UIElement.OpacityProperty,
            fade
        );
    }

    public static void StopSleepParticles(
        UIElement particles,
        TextBlock z1,
        TextBlock z2,
        TextBlock z3)
    {
        particles.Visibility = Visibility.Collapsed;

        foreach (TextBlock z in new[]
        {
        z1, z2, z3
    })
        {
            // Stop the visibility animation
            z.BeginAnimation(
                UIElement.OpacityProperty,
                null
            );

            // Reset the particle
            z.Opacity = 0;
            z.RenderTransform = Transform.Identity;
        }
    }

    public static void StartHeartParticles(
        UIElement particles,
        TextBlock heart1,
        TextBlock heart2,
        TextBlock heart3)
    {
        particles.Visibility = Visibility.Visible;

        // Hearts float upward at different moments
        AnimateHeart(heart1, 0.0, -18);
        AnimateHeart(heart2, 0.25, 0);
        AnimateHeart(heart3, 0.5, 18);
    }

    private static void AnimateHeart(
        TextBlock heart,
        double delay,
        double driftX)
    {
        var duration = TimeSpan.FromSeconds(1.4);
        var startDelay = TimeSpan.FromSeconds(delay);

        // Scaling and movement
        var scale = new ScaleTransform(0.7, 0.7);
        var movement = new TranslateTransform();

        var transforms = new TransformGroup();
        transforms.Children.Add(scale);
        transforms.Children.Add(movement);

        heart.RenderTransform = transforms;
        heart.RenderTransformOrigin = new Point(0.5, 0.5);

        // Float upward
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

        // Drift slightly sideways
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

        // Grow as the heart floats
        var grow = new DoubleAnimation
        {
            From = 0.7,
            To = 1.15,
            Duration = duration,
            BeginTime = startDelay,
            FillBehavior = FillBehavior.Stop
        };

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty, grow);

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty, grow);

        // Fade in, stay visible, then disappear
        var fade = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration,
            BeginTime = startDelay,
            FillBehavior = FillBehavior.Stop
        };

        fade.KeyFrames.Add(
            new DiscreteDoubleKeyFrame(
                0,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(0.18))));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                1,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromSeconds(0.85))));

        fade.KeyFrames.Add(
            new LinearDoubleKeyFrame(
                0,
                KeyTime.FromTimeSpan(duration)));

        heart.BeginAnimation(
            UIElement.OpacityProperty,
            fade
        );
    }

    public static void StopHeartParticles(
        UIElement particles,
        TextBlock heart1,
        TextBlock heart2,
        TextBlock heart3)
    {
        particles.Visibility = Visibility.Collapsed;

        foreach (TextBlock heart in new[]
        {
        heart1, heart2, heart3
    })
        {
            // Stop the opacity animation
            heart.BeginAnimation(
                UIElement.OpacityProperty,
                null
            );

            // Reset the particle
            heart.Opacity = 0;
            heart.RenderTransform = Transform.Identity;
        }
    }
}
