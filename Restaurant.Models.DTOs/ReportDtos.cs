using System;
using System.Collections.Generic;

namespace Restaurant.Models.DTOs;

/// <summary>
/// تقرير إحصائيات صنف (منتج) معين
/// </summary>
public class ProductReportDto
{
    /// <summary>
    /// معرف المنتج
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// اسم المنتج
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// باركود المنتج
    /// </summary>
    public string? BarCode { get; set; }

    /// <summary>
    /// الكمية الإجمالية المباعة
    /// </summary>
    public int QuantitySold { get; set; }

    /// <summary>
    /// إجمالي الإيرادات (المبيعات)
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// إجمالي تكلفة الكميات المباعة
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// إجمالي الربح المحقق
    /// </summary>
    public decimal TotalProfit { get; set; }
}

/// <summary>
/// تقرير إحصائيات تصنيف معين
/// </summary>
public class CategoryReportDto
{
    /// <summary>
    /// معرف التصنيف
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// اسم التصنيف
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// الكمية الإجمالية المباعة من منتجات هذا التصنيف
    /// </summary>
    public int QuantitySold { get; set; }

    /// <summary>
    /// إجمالي المبيعات
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// إجمالي التكلفة لمنتجات هذا التصنيف
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// إجمالي الربح لمنتجات هذا التصنيف
    /// </summary>
    public decimal TotalProfit { get; set; }
}

/// <summary>
/// تقرير المبيعات اليومية
/// </summary>
public class DailySalesReportDto
{
    /// <summary>
    /// تاريخ اليوم
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// إجمالي المبيعات لليوم
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// إجمالي التكلفة لليوم
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// إجمالي الربح لليوم
    /// </summary>
    public decimal TotalProfit { get; set; }

    /// <summary>
    /// عدد الطلبات المكتملة في هذا اليوم
    /// </summary>
    public int TotalOrders { get; set; }
}

/// <summary>
/// تقرير المبيعات حسب وسيلة الدفع
/// </summary>
public class PaymentMethodSalesReportDto
{
    /// <summary>
    /// معرف وسيلة الدفع
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// اسم وسيلة الدفع
    /// </summary>
    public string PaymentMethodName { get; set; } = null!;

    /// <summary>
    /// إجمالي المبيعات المحصلة بهذه الوسيلة
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// عدد الطلبات التي دفعت بهذه الوسيلة
    /// </summary>
    public int TotalOrders { get; set; }
}

/// <summary>
/// ملخص أعداد الطلبات حسب حالتها
/// </summary>
public class OrderStatusCountDto
{
    /// <summary>
    /// حالة الطلب (عربي/إنجليزي)
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// عدد الطلبات بهذه الحالة
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// إجمالي قيمة هذه الطلبات
    /// </summary>
    public decimal TotalSales { get; set; }
}

/// <summary>
/// ملخص أعداد الطلبات حسب نوعها
/// </summary>
public class OrderTypeCountDto
{
    /// <summary>
    /// نوع الطلب (داخل المطعم، خارج المطعم، توصيل)
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// عدد الطلبات بهذا النوع
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// إجمالي قيمة هذه الطلبات
    /// </summary>
    public decimal TotalSales { get; set; }
}

/// <summary>
/// تفاصيل مبسطة للطلب الملغى
/// </summary>
public class CancelledOrderDto
{
    /// <summary>
    /// معرف الطلب
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// رقم الطلب
    /// </summary>
    public int OrderNumber { get; set; }

    /// <summary>
    /// تاريخ ووقت الطلب
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// إجمالي قيمة الطلب الملغى
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// الملاحظات أو سبب الإلغاء
    /// </summary>
    public string Notes { get; set; } = null!;

    /// <summary>
    /// اسم المستخدم الذي قام بالعملية
    /// </summary>
    public string UserName { get; set; } = null!;
}

/// <summary>
/// التقرير الشامل والمؤشرات الرئيسية (Dashboard API)
/// </summary>
public class ComprehensiveReportDto
{
    /// <summary>
    /// إجمالي المبيعات (الطلبات غير الملغاة)
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// إجمالي التكلفة
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// إجمالي الأرباح المحققة
    /// </summary>
    public decimal TotalProfit { get; set; }

    /// <summary>
    /// إجمالي الخصومات الممنوحة
    /// </summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>
    /// إجمالي عدد الطلبات (غير الملغاة)
    /// </summary>
    public int TotalOrdersCount { get; set; }

    /// <summary>
    /// متوسط قيمة الطلب الواحد
    /// </summary>
    public decimal AverageOrderValue => TotalOrdersCount > 0 ? TotalSales / TotalOrdersCount : 0;

    /// <summary>
    /// المبيعات اليومية
    /// </summary>
    public List<DailySalesReportDto> DailySales { get; set; } = new();

    /// <summary>
    /// المبيعات حسب وسيلة الدفع
    /// </summary>
    public List<PaymentMethodSalesReportDto> SalesByPaymentMethod { get; set; } = new();

    /// <summary>
    /// الأصناف الأكثر مبيعاً
    /// </summary>
    public List<ProductReportDto> BestSellingProducts { get; set; } = new();

    /// <summary>
    /// التصنيفات الأكثر مبيعاً
    /// </summary>
    public List<CategoryReportDto> BestSellingCategories { get; set; } = new();

    /// <summary>
    /// ملخص حالات الطلبات
    /// </summary>
    public List<OrderStatusCountDto> OrderStatusSummary { get; set; } = new();

    /// <summary>
    /// ملخص أنواع الطلبات
    /// </summary>
    public List<OrderTypeCountDto> OrderTypeSummary { get; set; } = new();

    /// <summary>
    /// عدد الطلبات التي تم إلغاؤها
    /// </summary>
    public int CancelledOrdersCount { get; set; }
}
