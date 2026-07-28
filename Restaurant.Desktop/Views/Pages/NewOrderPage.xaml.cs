using System;
using System.Windows;
using System.Windows.Controls;
using Restaurant.Desktop.ViewModels;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Views.Pages
{
    public partial class NewOrderPage : UserControl
    {
        public NewOrderPage()
        {
            InitializeComponent();
        }

        private void CategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NewOrderViewModel vm && sender is RadioButton radio)
            {
                if (radio.Tag?.ToString() == "All")
                {
                    vm.SelectedCategory = null;
                }
                else if (radio.DataContext is CategoryDto category)
                {
                    vm.SelectedCategory = category;
                }
            }
        }

        private void OrderType_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is NewOrderViewModel vm && sender is RadioButton radio && radio.Tag != null)
            {
                string tag = radio.Tag.ToString() ?? "";
                if (Enum.TryParse<OrderType>(tag, out var orderType))
                {
                    vm.SelectedOrderType = orderType;
                }
            }
        }

        private void ProductCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is ProductDto product)
            {
                if (DataContext is NewOrderViewModel vm)
                {
                    vm.AddToCartCommand.Execute(product);
                }
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CartItemModel item)
            {
                if (DataContext is NewOrderViewModel vm)
                {
                    vm.DecreaseQuantityCommand.Execute(item);
                }
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CartItemModel item)
            {
                if (DataContext is NewOrderViewModel vm)
                {
                    vm.IncreaseQuantityCommand.Execute(item);
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CartItemModel item)
            {
                if (DataContext is NewOrderViewModel vm)
                {
                    vm.RemoveFromCartCommand.Execute(item);
                }
            }
        }

        private void btnedit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CartItemModel item)
            {
                if (DataContext is NewOrderViewModel vm)
                {
                    vm.EditCartItemCommand.Execute(item);
                }
            }
        }
    }
}
