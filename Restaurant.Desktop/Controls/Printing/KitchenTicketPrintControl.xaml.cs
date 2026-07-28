using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Controls.Printing
{
    /// <summary>
    /// Interaction logic for KitchenTicketPrintControl.xaml
    /// </summary>
    public partial class KitchenTicketPrintControl : UserControl
    {
        public KitchenTicketPrintControl()
        {
            InitializeComponent();
        }

        public void SetOrder(OrderDto order, string stationName, IEnumerable<OrderItemDto> items)
        {
            string orderTypeStr = order.OrderType switch
            {
                OrderType.Indoor => "داخلي 🟢",
                OrderType.Outdoor => "سفري سفري 🟡",
                OrderType.Delivery => "توصيل 🔴",
                _ => "غير محدد"
            };

            // Distinct background color brushes depending on order type
            Brush bgBrush = order.OrderType switch
            {
                OrderType.Indoor => new SolidColorBrush(Color.FromRgb(226, 240, 217)), // Light Green
                OrderType.Outdoor => new SolidColorBrush(Color.FromRgb(255, 242, 204)), // Light Yellow
                OrderType.Delivery => new SolidColorBrush(Color.FromRgb(252, 228, 214)), // Light Red
                _ => Brushes.White
            };

            var printItems = items.Select(item => new KitchenItemPrintModel
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Notes = item.Notes
            }).ToList();

            var viewModel = new KitchenTicketPrintViewModel
            {
                StationName = stationName,
                OrderNumber = order.OrderNumber.ToString(),
                FormattedTime = order.Date.ToString("HH:mm yyyy/MM/dd"),
                OrderTypeArabic = orderTypeStr,
                OrderTypeBgBrush = bgBrush,
                Notes = order.Notes,
                OrderItems = printItems
            };

            this.DataContext = viewModel;
        }
    }

    public class KitchenTicketPrintViewModel
    {
        public string StationName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string FormattedTime { get; set; } = string.Empty;
        public string OrderTypeArabic { get; set; } = string.Empty;
        public Brush OrderTypeBgBrush { get; set; } = Brushes.Transparent;
        public string Notes { get; set; } = string.Empty;

        public bool HasOrderNotes => !string.IsNullOrWhiteSpace(Notes) && Notes != "لا يوجد ملاحظات";

        public List<KitchenItemPrintModel> OrderItems { get; set; } = new();
    }

    public class KitchenItemPrintModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;

        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes) && Notes != "لا يوجد ملاحظات";
    }
}
