using System.Windows;
using Restaurant.Desktop.ViewModels;
using Restaurant.Desktop.Services;

namespace Restaurant.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly IToastService _toastService;

        public MainWindow(MainViewModel viewModel, IToastService toastService)
        {
            InitializeComponent();
            DataContext = viewModel;

            _toastService = toastService;
            _toastService.OnShow += (message, type) =>
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationToast.Show(message, type);
                });
            };
        }

        private void UserMenuButton_Click(object sender, RoutedEventArgs e)
        {
            UserPopup.IsOpen = !UserPopup.IsOpen;
        }

        private void UserMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UserPopup.IsOpen = false;
        }
    }
}