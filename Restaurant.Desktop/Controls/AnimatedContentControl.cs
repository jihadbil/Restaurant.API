using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Restaurant.Desktop.Controls
{
    public class AnimatedContentControl : ContentControl
    {
        private readonly TranslateTransform _translateTransform;

        public AnimatedContentControl()
        {
            _translateTransform = new TranslateTransform();
            this.RenderTransform = _translateTransform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // Reset state to avoid visual jumps
            this.Opacity = 0;
            _translateTransform.Y = 12;

            // Define animations
            var opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var translateAnim = new DoubleAnimation(12.0, 0.0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Start animations on this control
            this.BeginAnimation(OpacityProperty, opacityAnim);
            _translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
        }
    }
}
