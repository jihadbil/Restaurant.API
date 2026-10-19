using Microsoft.EntityFrameworkCore;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;

namespace Restaurant.Services;

/// <summary>Financial reports use dated ledger events, including refunds, rather than the order's current status.</summary>
public class ReportService(IUnitOfWork unitOfWork) : IReportService
{
    private IQueryable<CashDrawerEntry> Entries(ReportPeriod period) => unitOfWork.CashDrawerEntries.Query()
        .Where(e => e.Date >= period.Start && e.Date < period.EndExclusive && e.OrderId != null
            && (e.EntryType == CashDrawerEntryType.SalePayment || e.EntryType == CashDrawerEntryType.Refund));

    public async Task<ComprehensiveReportDto> GetComprehensiveReportAsync(DateTime startDate, DateTime endDate)
    {
        var period = ReportPeriod.Create(startDate, endDate);
        var daily = (await GetDailySalesAsync(startDate, endDate)).ToList();
        var orders = unitOfWork.Orders.Query().Where(o => o.Date >= period.Start && o.Date < period.EndExclusive);
        var statusRows = await orders.GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) }).ToListAsync();
        var typeRows = await orders.GroupBy(o => o.OrderType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) }).ToListAsync();
        var paidOrderIds = Entries(period).Where(entry => entry.EntryType == CashDrawerEntryType.SalePayment)
            .Select(entry => entry.OrderId);
        var discounts = await unitOfWork.Orders.Query().Where(order => paidOrderIds.Contains(order.Id))
            .Select(order => order.Discount + order.OrderItems.Sum(i => i.UnitDiscount * i.Quantity)).ToListAsync();
        return new ComprehensiveReportDto
        {
            TotalSales = daily.Sum(d => d.TotalSales), TotalCost = daily.Sum(d => d.TotalCost),
            TotalProfit = daily.Sum(d => d.TotalProfit), GrossSales = daily.Sum(d => d.GrossSales),
            TotalRefunds = daily.Sum(d => d.TotalRefunds), TotalDiscount = discounts.Sum(),
            TotalOrdersCount = await Entries(period).Where(e => e.EntryType == CashDrawerEntryType.SalePayment)
                .Select(e => e.OrderId).Distinct().CountAsync(),
            CancelledOrdersCount = await unitOfWork.Orders.Query().CountAsync(o => o.CancelledAt >= period.Start && o.CancelledAt < period.EndExclusive),
            DailySales = daily,
            SalesByPaymentMethod = (await GetSalesByPaymentMethodAsync(startDate, endDate)).ToList(),
            BestSellingProducts = (await GetBestSellingProductsAsync(startDate, endDate, 5)).ToList(),
            BestSellingCategories = (await GetBestSellingCategoriesAsync(startDate, endDate, 5)).ToList(),
            OrderStatusSummary = statusRows.Select(r => new OrderStatusCountDto { Status = StatusText(r.Status), Count = r.Count, TotalSales = r.Total }).ToList(),
            OrderTypeSummary = typeRows.Select(r => new OrderTypeCountDto { Type = TypeText(r.Type), Count = r.Count, TotalSales = r.Total }).ToList()
        };
    }

    public async Task<IEnumerable<DailySalesReportDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate)
    {
        var rows = await Entries(ReportPeriod.Create(startDate, endDate)).GroupBy(e => e.Date.Date)
            .Select(g => new DailySalesReportDto
            {
                Date = g.Key,
                GrossSales = g.Sum(e => e.EntryType == CashDrawerEntryType.SalePayment ? e.Amount : 0),
                TotalRefunds = g.Sum(e => e.EntryType == CashDrawerEntryType.Refund ? e.Amount : 0),
                TotalSales = g.Sum(e => e.EntryType == CashDrawerEntryType.Refund ? -e.Amount : e.Amount),
                TotalCost = g.Sum(e => e.Order!.Total == 0 ? 0 : e.Order.Cost * (e.Amount / e.Order.Total)
                    * (e.EntryType == CashDrawerEntryType.Refund ? -1 : 1)),
                TotalOrders = g.Where(e => e.EntryType == CashDrawerEntryType.SalePayment).Select(e => e.OrderId).Distinct().Count()
            }).OrderBy(d => d.Date).ToListAsync();
        foreach (var row in rows)
        {
            row.TotalCost = decimal.Round(row.TotalCost, 2, MidpointRounding.AwayFromZero);
            row.TotalProfit = row.TotalSales - row.TotalCost;
        }
        return rows;
    }

    private IQueryable<LineEvent> Lines(ReportPeriod period) =>
        from payment in Entries(period).GroupBy(entry => entry.OrderId)
            .Select(g => new { OrderId = g.Key,
                NetAmount = g.Sum(entry => entry.EntryType == CashDrawerEntryType.Refund ? -entry.Amount : entry.Amount) })
        join order in unitOfWork.Orders.Query() on payment.OrderId equals order.Id
        from item in order.OrderItems
        select new LineEvent
        {
            ProductId = item.ProductId,
            ProductName = item.ProductNameSnapshot ?? item.Product.Name,
            BarCode = item.ProductBarCodeSnapshot,
            CategoryId = item.CategoryIdSnapshot ?? item.Product.CategoryId,
            CategoryName = item.CategoryNameSnapshot ?? item.Product.Category.Name,
            Quantity = order.Total == 0 ? 0 : item.Quantity * (payment.NetAmount / order.Total),
            Revenue = order.Total == 0 ? 0 : (item.Total - item.OrderDiscountShare) * (payment.NetAmount / order.Total),
            Cost = order.Total == 0 ? 0 : item.Quantity * item.UnitCostPrice * (payment.NetAmount / order.Total)
        };

    public async Task<IEnumerable<ProductReportDto>> GetBestSellingProductsAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        ValidateLimit(limit);
        var rows = await Lines(ReportPeriod.Create(startDate, endDate))
            .GroupBy(i => new { i.ProductId, i.ProductName, i.BarCode })
            .Select(g => new ProductReportDto
            {
                ProductId = g.Key.ProductId, Name = g.Key.ProductName, BarCode = g.Key.BarCode,
                QuantitySold = g.Sum(i => i.Quantity), TotalRevenue = g.Sum(i => i.Revenue), TotalCost = g.Sum(i => i.Cost)
            }).ToListAsync();
        foreach (var row in rows)
        {
            row.QuantitySold = decimal.Round(row.QuantitySold, 6, MidpointRounding.AwayFromZero);
            row.TotalRevenue = decimal.Round(row.TotalRevenue, 2, MidpointRounding.AwayFromZero);
            row.TotalCost = decimal.Round(row.TotalCost, 2, MidpointRounding.AwayFromZero);
            row.TotalProfit = row.TotalRevenue - row.TotalCost;
        }
        return rows.OrderByDescending(p => p.QuantitySold).Take(limit);
    }

    public async Task<IEnumerable<CategoryReportDto>> GetBestSellingCategoriesAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        ValidateLimit(limit);
        var rows = await Lines(ReportPeriod.Create(startDate, endDate))
            .GroupBy(i => new { i.CategoryId, i.CategoryName })
            .Select(g => new CategoryReportDto
            {
                CategoryId = g.Key.CategoryId, Name = g.Key.CategoryName,
                QuantitySold = g.Sum(i => i.Quantity), TotalRevenue = g.Sum(i => i.Revenue), TotalCost = g.Sum(i => i.Cost)
            }).ToListAsync();
        foreach (var row in rows)
        {
            row.QuantitySold = decimal.Round(row.QuantitySold, 6, MidpointRounding.AwayFromZero);
            row.TotalRevenue = decimal.Round(row.TotalRevenue, 2, MidpointRounding.AwayFromZero);
            row.TotalCost = decimal.Round(row.TotalCost, 2, MidpointRounding.AwayFromZero);
            row.TotalProfit = row.TotalRevenue - row.TotalCost;
        }
        return rows.OrderByDescending(p => p.QuantitySold).Take(limit);
    }

    public async Task<IEnumerable<PaymentMethodSalesReportDto>> GetSalesByPaymentMethodAsync(DateTime startDate, DateTime endDate)
    {
        var rows = await Entries(ReportPeriod.Create(startDate, endDate))
            .GroupBy(e => new { e.PaymentMethodId, Name = e.PaymentMethodNameSnapshot ?? (e.PaymentMethod == null ? "طريقة محذوفة" : e.PaymentMethod.Name) })
            .Select(g => new PaymentMethodSalesReportDto
            {
                PaymentMethodId = g.Key.PaymentMethodId ?? 0, PaymentMethodName = g.Key.Name,
                GrossSales = g.Sum(e => e.EntryType == CashDrawerEntryType.SalePayment ? e.Amount : 0),
                TotalRefunds = g.Sum(e => e.EntryType == CashDrawerEntryType.Refund ? e.Amount : 0),
                TotalSales = g.Sum(e => e.EntryType == CashDrawerEntryType.Refund ? -e.Amount : e.Amount),
                TotalOrders = g.Where(e => e.EntryType == CashDrawerEntryType.SalePayment).Select(e => e.OrderId).Distinct().Count()
            }).ToListAsync();
        return rows.OrderByDescending(p => p.TotalSales);
    }

    public async Task<IEnumerable<CancelledOrderDto>> GetCancelledOrdersAsync(DateTime startDate, DateTime endDate)
    {
        var period = ReportPeriod.Create(startDate, endDate);
        return await unitOfWork.Orders.Query().Where(o => o.OrderStatus == OrderStatus.Cancelled
                && (o.CancelledAt ?? o.Date) >= period.Start && (o.CancelledAt ?? o.Date) < period.EndExclusive)
            .OrderByDescending(o => o.CancelledAt ?? o.Date).Select(o => new CancelledOrderDto
            {
                OrderId = o.Id, OrderNumber = o.OrderNumber, Date = o.CancelledAt ?? o.Date,
                Total = o.Total, Notes = o.Notes, UserName = o.CancelledByUserName ?? o.User.UserName ?? "غير معروف"
            }).ToListAsync();
    }

    private static void ValidateLimit(int limit)
    {
        if (limit < 1 || limit > 500) throw new ArgumentException("عدد النتائج يجب أن يكون بين 1 و500.");
    }
    private sealed class LineEvent
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? BarCode { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
    }
    private static string StatusText(OrderStatus? status) => status switch
    {
        OrderStatus.Preparing => "قيد التحضير", OrderStatus.Ready => "جاهز للتسليم",
        OrderStatus.Delivered => "تم التسليم", OrderStatus.Cancelled => "ملغى", _ => "غير محدد"
    };
    private static string TypeText(OrderType? type) => type switch
    {
        OrderType.Indoor => "داخل المطعم", OrderType.Outdoor => "خارج المطعم", OrderType.Delivery => "توصيل", _ => "غير محدد"
    };
}
