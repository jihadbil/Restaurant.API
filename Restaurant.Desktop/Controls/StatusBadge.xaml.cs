using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Controls
{
    public partial class StatusBadge : UserControl
    {
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(OrderStatus?),
                typeof(StatusBadge),
                new PropertyMetadata(null, OnStatusChanged));

        public OrderStatus? Status
        {
            get => (OrderStatus?)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public StatusBadge()
        {
            InitializeComponent();
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge)
            {
                badge.UpdateStatus((OrderStatus?)e.NewValue);
            }
        }

        private void UpdateStatus(OrderStatus? status)
        {
            if (status == null)
            {
                badgeBorder.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246));
                badgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                badgeText.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                badgeText.Text = "غير محدد";
                return;
            }

            switch (status.Value)
            {
                case OrderStatus.Preparing:
                    badgeBorder.Background = new SolidColorBrush(Color.FromArgb(25, 245, 158, 11)); // 10% Warning Color
                    badgeBorder.BorderBrush = (SolidColorBrush)FindResource("BrushWarning");
                    badgeText.Foreground = (SolidColorBrush)FindResource("BrushWarning");
                    badgeText.Text = "قيد التحضير";
                    break;
                case OrderStatus.Ready:
                    badgeBorder.Background = new SolidColorBrush(Color.FromArgb(25, 59, 130, 246)); // 10% Info Color
                    badgeBorder.BorderBrush = (SolidColorBrush)FindResource("BrushInfo");
                    badgeText.Foreground = (SolidColorBrush)FindResource("BrushInfo");
                    badgeText.Text = "جاهز للتسليم";
                    break;
                case OrderStatus.Delivered:
                    badgeBorder.Background = new SolidColorBrush(Color.FromArgb(25, 16, 185, 129)); // 10% Success Color
                    badgeBorder.BorderBrush = (SolidColorBrush)FindResource("BrushSuccess");
                    badgeText.Foreground = (SolidColorBrush)FindResource("BrushSuccess");
                    badgeText.Text = "تم التسليم";
                    break;
                case OrderStatus.Cancelled:
                    badgeBorder.Background = new SolidColorBrush(Color.FromArgb(25, 239, 68, 68)); // 10% Danger Color
                    badgeBorder.BorderBrush = (SolidColorBrush)FindResource("BrushDanger");
                    badgeText.Foreground = (SolidColorBrush)FindResource("BrushDanger");
                    badgeText.Text = "ملغى";
                    break;
            }
        }
    }
}
