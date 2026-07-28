using System.Windows;
using Restaurant.Desktop.ViewModels;

namespace Restaurant.Desktop.Views
{
    public partial class ShiftCloseWindow : Window
    {
        public ShiftCloseWindow(ShiftCloseViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
