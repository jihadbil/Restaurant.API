using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

/// <summary>
/// تنفيذ خدمة التقارير والإحصائيات
/// </summary>
public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// الحصول على التقرير الشامل والمؤشرات الرئيسية (Dashboard API)
    /// </summary>
    public async Task<ComprehensiveReportDto> GetComprehensiveReportAsync(DateTime startDate, DateTime endDate)
    {
        // تهيئة التواريخ لتشمل اليوم بالكامل
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        // جلب جميع الطلبات في الفترة المحددة مع البيانات المرتبطة بها
        var allOrders = (await _unitOfWork.Orders.GetAllAsync(
            filter: o => o.Date >= start && o.Date <= end,
            includeProperties: "User,OrderItems,OrderItems.Product,OrderItems.Product.Category"
        )).ToList();

        // الطلبات الفعالة (غير الملغاة)
        var activeOrders = allOrders.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();

        var report = new ComprehensiveReportDto
        {
            TotalSales = activeOrders.Sum(o => o.Total),
            TotalCost = activeOrders.Sum(o => o.Cost),
            TotalProfit = activeOrders.Sum(o => o.Profit),
            TotalDiscount = activeOrders.Sum(o => o.Discount),
            TotalOrdersCount = activeOrders.Count,
            CancelledOrdersCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled)
        };

        // 1. المبيعات اليومية
        report.DailySales = activeOrders
            .GroupBy(o => o.Date.Date)
            .Select(g => new DailySalesReportDto
            {
                Date = g.Key,
                TotalSales = g.Sum(o => o.Total),
                TotalCost = g.Sum(o => o.Cost),
                TotalProfit = g.Sum(o => o.Profit),
                TotalOrders = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // 2. المبيعات حسب وسيلة الدفع
        var salesEntries = (await _unitOfWork.CashDrawerEntries.GetAllAsync(
            filter: e => e.Date >= start && e.Date <= end && e.EntryType == CashDrawerEntryType.SalePayment && e.PaymentMethodId != null,
            includeProperties: "PaymentMethod,Order"
        )).ToList();

        var activeSalesEntries = salesEntries.Where(e => e.Order != null && e.Order.OrderStatus != OrderStatus.Cancelled).ToList();

        report.SalesByPaymentMethod = activeSalesEntries
            .GroupBy(e => new { e.PaymentMethodId, Name = e.PaymentMethod != null ? e.PaymentMethod.Name : "غير معروف" })
            .Select(g => new PaymentMethodSalesReportDto
            {
                PaymentMethodId = g.Key.PaymentMethodId ?? 0,
                PaymentMethodName = g.Key.Name,
                TotalSales = g.Sum(e => e.Amount),
                TotalOrders = g.Select(e => e.OrderId).Distinct().Count()
            })
            .OrderByDescending(p => p.TotalSales)
            .ToList();

        // 3. الأصناف الأكثر مبيعاً (أعلى 5 منتجات)
        var activeOrderItems = activeOrders.SelectMany(o => o.OrderItems).ToList();
        report.BestSellingProducts = activeOrderItems
            .Where(oi => oi.Product != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.BarCode })
            .Select(g => new ProductReportDto
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.Name,
                BarCode = g.Key.BarCode,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Total),
                TotalCost = g.Sum(oi => oi.Quantity * oi.UnitCostPrice),
                TotalProfit = g.Sum(oi => oi.Total) - g.Sum(oi => oi.Quantity * oi.UnitCostPrice)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        // 4. التصنيفات الأكثر مبيعاً (أعلى 5 تصنيفات)
        report.BestSellingCategories = activeOrderItems
            .Where(oi => oi.Product != null && oi.Product.Category != null)
            .GroupBy(oi => new { oi.Product.Category.Id, oi.Product.Category.Name })
            .Select(g => new CategoryReportDto
            {
                CategoryId = g.Key.Id,
                Name = g.Key.Name,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Total),
                TotalCost = g.Sum(oi => oi.Quantity * oi.UnitCostPrice),
                TotalProfit = g.Sum(oi => oi.Total) - g.Sum(oi => oi.Quantity * oi.UnitCostPrice)
            })
            .OrderByDescending(c => c.QuantitySold)
            .Take(5)
            .ToList();

        // 5. ملخص حالات الطلبات (عربي)
        report.OrderStatusSummary = allOrders
            .GroupBy(o => o.OrderStatus)
            .Select(g => new OrderStatusCountDto
            {
                Status = GetArabicOrderStatus(g.Key),
                Count = g.Count(),
                TotalSales = g.Sum(o => o.Total)
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // 6. ملخص أنواع الطلبات (عربي)
        report.OrderTypeSummary = activeOrders
            .GroupBy(o => o.OrderType)
            .Select(g => new OrderTypeCountDto
            {
                Type = GetArabicOrderType(g.Key),
                Count = g.Count(),
                TotalSales = g.Sum(o => o.Total)
            })
            .OrderByDescending(t => t.Count)
            .ToList();

        return report;
    }

    /// <summary>
    /// الأصناف الأكثر مبيعاً بفترة معينة
    /// </summary>
    public async Task<IEnumerable<ProductReportDto>> GetBestSellingProductsAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        var activeOrders = await _unitOfWork.Orders.GetAllAsync(
            filter: o => o.Date >= start && o.Date <= end && o.OrderStatus != OrderStatus.Cancelled,
            includeProperties: "OrderItems,OrderItems.Product"
        );

        var items = activeOrders.SelectMany(o => o.OrderItems);

        return items
            .Where(oi => oi.Product != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.BarCode })
            .Select(g => new ProductReportDto
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.Name,
                BarCode = g.Key.BarCode,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Total),
                TotalCost = g.Sum(oi => oi.Quantity * oi.UnitCostPrice),
                TotalProfit = g.Sum(oi => oi.Total) - g.Sum(oi => oi.Quantity * oi.UnitCostPrice)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// التصنيفات الأكثر مبيعاً بفترة معينة
    /// </summary>
    public async Task<IEnumerable<CategoryReportDto>> GetBestSellingCategoriesAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        var activeOrders = await _unitOfWork.Orders.GetAllAsync(
            filter: o => o.Date >= start && o.Date <= end && o.OrderStatus != OrderStatus.Cancelled,
            includeProperties: "OrderItems,OrderItems.Product,OrderItems.Product.Category"
        );

        var items = activeOrders.SelectMany(o => o.OrderItems);

        return items
            .Where(oi => oi.Product != null && oi.Product.Category != null)
            .GroupBy(oi => new { oi.Product.Category.Id, oi.Product.Category.Name })
            .Select(g => new CategoryReportDto
            {
                CategoryId = g.Key.Id,
                Name = g.Key.Name,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Total),
                TotalCost = g.Sum(oi => oi.Quantity * oi.UnitCostPrice),
                TotalProfit = g.Sum(oi => oi.Total) - g.Sum(oi => oi.Quantity * oi.UnitCostPrice)
            })
            .OrderByDescending(c => c.QuantitySold)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// المبيعات اليومية في فترة معينة
    /// </summary>
    public async Task<IEnumerable<DailySalesReportDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        var activeOrders = await _unitOfWork.Orders.GetAllAsync(
            filter: o => o.Date >= start && o.Date <= end && o.OrderStatus != OrderStatus.Cancelled
        );

        return activeOrders
            .GroupBy(o => o.Date.Date)
            .Select(g => new DailySalesReportDto
            {
                Date = g.Key,
                TotalSales = g.Sum(o => o.Total),
                TotalCost = g.Sum(o => o.Cost),
                TotalProfit = g.Sum(o => o.Profit),
                TotalOrders = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    /// <summary>
    /// المبيعات حسب طرق الدفع بفترة معينة
    /// </summary>
    public async Task<IEnumerable<PaymentMethodSalesReportDto>> GetSalesByPaymentMethodAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        var salesEntries = await _unitOfWork.CashDrawerEntries.GetAllAsync(
            filter: e => e.Date >= start && e.Date <= end && e.EntryType == CashDrawerEntryType.SalePayment && e.PaymentMethodId != null,
            includeProperties: "PaymentMethod,Order"
        );

        var activeSalesEntries = salesEntries.Where(e => e.Order != null && e.Order.OrderStatus != OrderStatus.Cancelled);

        return activeSalesEntries
            .GroupBy(e => new { e.PaymentMethodId, Name = e.PaymentMethod != null ? e.PaymentMethod.Name : "غير معروف" })
            .Select(g => new PaymentMethodSalesReportDto
            {
                PaymentMethodId = g.Key.PaymentMethodId ?? 0,
                PaymentMethodName = g.Key.Name,
                TotalSales = g.Sum(e => e.Amount),
                TotalOrders = g.Select(e => e.OrderId).Distinct().Count()
            })
            .OrderByDescending(p => p.TotalSales)
            .ToList();
    }

    /// <summary>
    /// الطلبات الملغاة بفترة معينة
    /// </summary>
    public async Task<IEnumerable<CancelledOrderDto>> GetCancelledOrdersAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddTicks(-1);

        var cancelledOrders = await _unitOfWork.Orders.GetAllAsync(
            filter: o => o.Date >= start && o.Date <= end && o.OrderStatus == OrderStatus.Cancelled,
            includeProperties: "User"
        );

        return cancelledOrders
            .Select(o => new CancelledOrderDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                Date = o.Date,
                Total = o.Total,
                Notes = o.Notes,
                UserName = o.User != null ? o.User.UserName! : "غير معروف"
            })
            .OrderByDescending(o => o.Date)
            .ToList();
    }

    // دوال مساعدة لترجمة الحالات والأنواع للغة العربية
    private string GetArabicOrderStatus(OrderStatus? status)
    {
        return status switch
        {
            OrderStatus.Preparing => "قيد التحضير",
            OrderStatus.Ready => "جاهز للتسليم",
            OrderStatus.Delivered => "تم التسليم",
            OrderStatus.Cancelled => "ملغى",
            _ => "غير محدد"
        };
    }

    private string GetArabicOrderType(OrderType? type)
    {
        return type switch
        {
            OrderType.Indoor => "داخل المطعم",
            OrderType.Outdoor => "خارج المطعم",
            OrderType.Delivery => "توصيل",
            _ => "غير محدد"
        };
    }
}
