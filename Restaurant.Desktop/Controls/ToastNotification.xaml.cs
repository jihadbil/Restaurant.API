using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Restaurant.Desktop.Controls
{
    public partial class ToastNotification : UserControl
    {
        public ToastNotification()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void Show(string message, string type)
        {
            txtMessage.Text = message;

            // Apply styling based on type (Success, Error, Warning, Info)
            var brushBorder = (SolidColorBrush)Application.Current.Resources["BrushBorder"];
            var brushPrimary = (SolidColorBrush)Application.Current.Resources["BrushPrimary"];
            var brushPrimaryAlpha8 = (SolidColorBrush)Application.Current.Resources["BrushPrimaryAlpha8"];
            
            var brushDanger = (SolidColorBrush)Application.Current.Resources["BrushDanger"];
            var brushDangerBg = (SolidColorBrush)Application.Current.Resources["BrushDangerBg"];
            
            var brushWarning = (SolidColorBrush)Application.Current.Resources["BrushWarning"];
            var brushWarningBg = (SolidColorBrush)Application.Current.Resources["BrushWarningBg"];
            
            var brushInfoBg = (SolidColorBrush)Application.Current.Resources["BrushInfoBg"];

            // Geometry path icons
            var checkGeometry = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");
            var errorGeometry = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z");
            var warnGeometry = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");
            var infoGeometry = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");

            switch (type.ToLower())
            {
                case "success":
                    border.BorderBrush = brushPrimary;
                    iconBorder.Background = brushPrimaryAlpha8;
                    iconPath.Fill = brushPrimary;
                    iconPath.Data = checkGeometry;
                    break;
                case "error":
                    border.BorderBrush = brushDanger;
                    iconBorder.Background = brushDangerBg;
                    iconPath.Fill = brushDanger;
                    iconPath.Data = errorGeometry;
                    break;
                case "warning":
                    border.BorderBrush = brushWarning;
                    iconBorder.Background = brushWarningBg;
                    iconPath.Fill = brushWarning;
                    iconPath.Data = warnGeometry;
                    break;
                default: // Info
                    border.BorderBrush = brushBorder;
                    iconBorder.Background = brushInfoBg;
                    iconPath.Fill = brushPrimary;
                    iconPath.Data = infoGeometry;
                    break;
            }

            // Slide in animation
            this.Visibility = Visibility.Visible;
            this.Opacity = 0;

            var translate = new TranslateTransform(0, 50);
            this.RenderTransform = translate;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            var opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300));
            var translateAnim = new DoubleAnimation(50.0, 0.0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(OpacityProperty, opacityAnim);
            translate.BeginAnimation(TranslateTransform.YProperty, translateAnim);

            // Auto dismiss timer
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Hide();
            };
            timer.Start();
        }

        public void Hide()
        {
            var translate = this.RenderTransform as TranslateTransform ?? new TranslateTransform();
            this.RenderTransform = translate;

            var opacityAnim = new DoubleAnimation(this.Opacity, 0.0, TimeSpan.FromMilliseconds(250));
            var translateAnim = new DoubleAnimation(translate.Y, 30.0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            opacityAnim.Completed += (s, e) => this.Visibility = Visibility.Collapsed;

            this.BeginAnimation(OpacityProperty, opacityAnim);
            translate.BeginAnimation(TranslateTransform.YProperty, translateAnim);
        }
    }
}
