// [LEGACY] معطّل — تم الانتقال إلى KitchenTicketPrintControl.xaml
/*
using System;
using System.Collections.Generic;
using System.Text;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Services.Printing
{
    public static class KitchenTicketBuilder
    {
        public static string Build(OrderDto order, string stationName, IEnumerable<OrderItemDto> items)
        {
            var sb = new StringBuilder();

            sb.AppendLine("==================================");
            sb.AppendLine("         طلب تحضير المطبخ         ");
            sb.AppendLine($"محطة الطباعة: {stationName}");
            sb.AppendLine("==================================");
            sb.AppendLine($"رقم الطلب: {order.OrderNumber}");
            sb.AppendLine($"الوقت: {order.Date:HH:mm}");
            
            string typeStr = order.OrderType switch
            {
                OrderType.Indoor => "داخلي",
                OrderType.Outdoor => "سفري",
                OrderType.Delivery => "توصيل",
                _ => "غير محدد"
            };
            sb.AppendLine($"نوع الطلب: {typeStr}");
            sb.AppendLine("----------------------------------");
            sb.AppendLine("المنتج                       الكمية");
            sb.AppendLine("----------------------------------");

            foreach (var item in items)
            {
                sb.AppendLine($"{item.ProductName,-25} x{item.Quantity}");
                if (!string.IsNullOrWhiteSpace(item.Notes) && item.Notes != "لا يوجد ملاحظات")
                {
                    sb.AppendLine($"  * ملاحظة: {item.Notes}");
                }
            }

            sb.AppendLine("==================================");
            if (!string.IsNullOrWhiteSpace(order.Notes) && order.Notes != "لا يوجد ملاحظات")
            {
                sb.AppendLine($"ملاحظات الطلب: {order.Notes}");
                sb.AppendLine("==================================");
            }

            return sb.ToString();
        }
    }
}
*/
