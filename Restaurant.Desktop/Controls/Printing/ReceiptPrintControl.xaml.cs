using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Controls.Printing
{
    /// <summary>
    /// Interaction logic for ReceiptPrintControl.xaml
    /// </summary>
    public partial class ReceiptPrintControl : UserControl
    {
        public ReceiptPrintControl()
        {
            InitializeComponent();
        }

        public void SetOrder(OrderDto order, string restaurantName)
        {
            string orderTypeStr = order.OrderType switch
            {
                OrderType.Indoor => "داخلي",
                OrderType.Outdoor => "سفري",
                OrderType.Delivery => "توصيل",
                _ => "غير محدد"
            };

            string paymentMethods = "غير محدد";
            if (order.CashDrawerEntries != null && order.CashDrawerEntries.Any())
            {
                var names = order.CashDrawerEntries
                    .Select(e => e.PaymentMethodName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();
                if (names.Any())
                {
                    paymentMethods = string.Join("، ", names);
                }
            }

            var items = new List<ReceiptItemPrintModel>();
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    items.Add(new ReceiptItemPrintModel
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitSalePrice = item.UnitSalePrice,
                        Total = item.Total,
                        Notes = item.Notes
                    });
                }
            }

            var viewModel = new ReceiptPrintViewModel
            {
                RestaurantName = restaurantName,
                OrderNumber = order.OrderNumber.ToString(),
                FormattedDate = order.Date.ToString("yyyy/MM/dd HH:mm"),
                OrderTypeArabic = orderTypeStr,
                UserName = order.UserName,
                PaymentMethods = paymentMethods,
                SubTotal = order.Total + order.Discount,
                Discount = order.Discount,
                Total = order.Total,
                Notes = order.Notes,
                OrderItems = items
            };

            this.DataContext = viewModel;
        }
    }

    public class ReceiptPrintViewModel
    {
        public string RestaurantName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
        public string OrderTypeArabic { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PaymentMethods { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string Notes { get; set; } = string.Empty;

        public bool HasDiscount => Discount > 0;
        public bool HasOrderNotes => !string.IsNullOrWhiteSpace(Notes) && Notes != "لا يوجد ملاحظات";

        public List<ReceiptItemPrintModel> OrderItems { get; set; } = new();
    }

    public class ReceiptItemPrintModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitSalePrice { get; set; }
        public decimal Total { get; set; }
        public string Notes { get; set; } = string.Empty;

        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes) && Notes != "لا يوجد ملاحظات";
    }
}
