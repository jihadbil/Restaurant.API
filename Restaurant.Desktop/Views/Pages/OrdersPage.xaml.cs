using System;
using System.Windows;
using System.Windows.Controls;
using Restaurant.Desktop.ViewModels;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Views.Pages
{
    public partial class OrdersPage : UserControl
    {
        public OrdersPage()
        {
            InitializeComponent();
        }

        private void RadioStatus_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm)
            {
                if (sender == RadioPrep)
                    vm.StatusFilter = "Preparing";
                else if (sender == RadioReady)
                    vm.StatusFilter = "Ready";
                else if (sender == RadioDelivered)
                    vm.StatusFilter = "Delivered";
                else if (sender == RadioCancelled)
                    vm.StatusFilter = "Cancelled";
                else
                    vm.StatusFilter = "All";
            }
        }

        private void RadioViewMode_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm)
            {
                if (sender == RadioCards)
                    vm.ViewMode = "Cards";
                else
                    vm.ViewMode = "Table";
            }
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm && sender is Button btn && btn.DataContext is OrderDto order)
            {
                vm.SelectedOrder = order;
            }
        }

        private async void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm && vm.SelectedOrder != null && sender is Button btn && btn.Tag is string newStatus)
            {
                string parameter = $"{vm.SelectedOrder.Id},{newStatus}";
                if (vm.ChangeStatusCommand.CanExecute(parameter))
                {
                    vm.ChangeStatusCommand.Execute(parameter);
                }
            }
        }
    }
}
