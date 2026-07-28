using System.Windows;
using System.Windows.Media;

namespace Restaurant.Desktop.Controls
{
    public partial class ConfirmWindow : Window
    {
        public bool Result { get; private set; }

        public ConfirmWindow(string title, string message, string confirmText, string type = "danger")
        {
            InitializeComponent();

            txtTitle.Text = title;
            txtMessage.Text = message;
            btnConfirm.Content = confirmText;

            var brushPrimary = (SolidColorBrush)Application.Current.Resources["BrushPrimary"];
            var brushPrimaryAlpha8 = (SolidColorBrush)Application.Current.Resources["BrushPrimaryAlpha8"];
            var brushDanger = (SolidColorBrush)Application.Current.Resources["BrushDanger"];
            var brushDangerBg = (SolidColorBrush)Application.Current.Resources["BrushDangerBg"];

            if (type.ToLower() == "primary")
            {
                btnConfirm.Style = (Style)Application.Current.Resources["ButtonStyle.Primary"];
                iconBorder.Background = brushPrimaryAlpha8;
                iconPath.Fill = brushPrimary;
                iconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");
            }
            else
            {
                btnConfirm.Style = (Style)Application.Current.Resources["ButtonStyle.Danger"];
                iconBorder.Background = brushDangerBg;
                iconPath.Fill = brushDanger;
                iconPath.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z");
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        public static bool Show(Window owner, string title, string message, string confirmText, string type = "danger")
        {
            var dialog = new ConfirmWindow(title, message, confirmText, type)
            {
                Owner = owner
            };
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
