using System.Windows;
using Restaurant.Desktop.ViewModels;

namespace Restaurant.Desktop.Views
{
    public partial class ActivationWindow : Window
    {
        public ActivationWindow(ActivationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
