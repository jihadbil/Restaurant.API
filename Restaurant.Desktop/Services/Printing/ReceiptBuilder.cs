// [LEGACY] معطّل — تم الانتقال إلى ReceiptPrintControl.xaml
/*
using System;
using System.Linq;
using System.Text;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Services.Printing
{
    public static class ReceiptBuilder
    {
        public static string Build(OrderDto order)
        {
            var sb = new StringBuilder();
            

            sb.AppendLine("==================================");
            sb.AppendLine("         فاتورة مبيعات            ");
            sb.AppendLine("==================================");
            sb.AppendLine($"رقم الطلب: {order.OrderNumber}");
            sb.AppendLine($"التاريخ: {order.Date:yyyy/MM/dd HH:mm}");
            
            string typeStr = order.OrderType switch
            {
                OrderType.Indoor => "داخلي",
                OrderType.Outdoor => "سفري",
                OrderType.Delivery => "توصيل",
                _ => "غير محدد"
            };
            sb.AppendLine($"نوع الطلب: {typeStr}");
            sb.AppendLine($"المستخدم: {order.UserName}");

            string paymentMethods = string.Empty;
            if (order.CashDrawerEntries != null && order.CashDrawerEntries.Any())
            {
                paymentMethods = string.Join(", ", order.CashDrawerEntries.Select(e => e.PaymentMethodName).Where(n => !string.IsNullOrEmpty(n)));
            }
            if (string.IsNullOrEmpty(paymentMethods))
            {
                paymentMethods = "غير محدد";
            }
            sb.AppendLine($"طريقة الدفع: {paymentMethods}");
            sb.AppendLine("----------------------------------");
            sb.AppendLine("المنتج  الكمية     السعر    الإجمالي");
            sb.AppendLine("----------------------------------");

            foreach (var item in order.OrderItems)
            {
                string productName = item.ProductName;
                if (productName.Length > 15)
                {
                    productName = productName.Substring(0, 12) + "...";
                }

                // Format: Product Name Qty Price Total
                sb.AppendLine($"{productName,-15} {item.Quantity,-5} {item.UnitSalePrice,6:F2} {item.Total,8:F2}");
                if (!string.IsNullOrWhiteSpace(item.Notes) && item.Notes != "لا يوجد ملاحظات")
                {
                    sb.AppendLine($"  * {item.Notes}");
                }
            }

            sb.AppendLine("----------------------------------");
            sb.AppendLine($"المجموع الفرعي: {order.Total + order.Discount:F2}");
            if (order.Discount > 0)
            {
                sb.AppendLine($"التخفيض: {order.Discount:F2}");
            }
            sb.AppendLine($"الصافي: {order.Total:F2}");
            sb.AppendLine("==================================");
            if (!string.IsNullOrWhiteSpace(order.Notes) && order.Notes != "لا يوجد ملاحظات")
            {
                sb.AppendLine($"ملاحظات: {order.Notes}");
                sb.AppendLine("==================================");
            }
            sb.AppendLine("      شكراً لزيارتكم وصحتين وعافية ");
            sb.AppendLine("==================================");

            return sb.ToString();
        }
    }
}
*/
