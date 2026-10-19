using Microsoft.EntityFrameworkCore;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Restaurant.Services.Tests;

public class AccountingRegressionTests
{
    [Fact]
    public async Task SplitPaymentsCountUnitsAndDiscountOnceAndProratePartialRefunds()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var request = TreasuryTests.Checkout(1);
        request.Order.Discount = 2m;
        var saved = await test.Orders.CheckoutAsync(request, "u", true);
        var sale = await test.Db.CashDrawerEntries.SingleAsync();
        sale.Amount = 9m;
        test.Db.CashDrawerEntries.Add(new CashDrawerEntry { OrderId = saved.Id, Amount = 9m,
            EntryType = CashDrawerEntryType.SalePayment, CashboxId = 1, PaymentMethodId = 2,
            UserId = "u", AffectsCash = false, Date = BusinessClock.Today.AddHours(12) });
        await test.Db.SaveChangesAsync();
        var reports = new ReportService(test.Unit);
        var complete = await reports.GetComprehensiveReportAsync(BusinessClock.Today, BusinessClock.Today);
        Assert.Equal(2m, Assert.Single(complete.BestSellingProducts).QuantitySold);
        Assert.Equal(2m, Assert.Single(complete.BestSellingCategories).QuantitySold);
        Assert.Equal(2m, complete.TotalDiscount);
        Assert.Equal(18m, complete.TotalSales);
        var tomorrow = BusinessClock.Today.AddDays(1);
        test.Db.CashDrawerEntries.Add(new CashDrawerEntry { OrderId = saved.Id, Amount = 4.5m,
            EntryType = CashDrawerEntryType.Refund, CashboxId = 1, PaymentMethodId = 2,
            UserId = "u", AffectsCash = false, Date = tomorrow.AddHours(12) });
        await test.Db.SaveChangesAsync();
        var refunded = Assert.Single(await reports.GetBestSellingProductsAsync(tomorrow, tomorrow));
        Assert.Equal(-0.5m, refunded.QuantitySold);
        Assert.Equal(-4.5m, refunded.TotalRevenue);
        Assert.Equal(-2m, refunded.TotalCost);
        var net = Assert.Single(await reports.GetBestSellingCategoriesAsync(BusinessClock.Today, tomorrow));
        Assert.Equal(1.5m, net.QuantitySold);
        Assert.Equal(13.5m, net.TotalRevenue);
    }

    [Fact]
    public async Task TreasuryPagingKeepsFullPeriodTotalsIncludingTransfersAndSurplus()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        test.Db.CashDrawerEntries.AddRange(Enumerable.Range(0, 125).Select(i => new CashDrawerEntry
        {
            Date = BusinessClock.Today.AddHours(12), Amount = 1, AffectsCash = true, CashboxId = 1, UserId = "u",
            EntryType = i % 2 == 0 ? CashDrawerEntryType.TransferIn : CashDrawerEntryType.CashSurplus
        }));
        await test.Db.SaveChangesAsync();
        var page = await test.Entries.GetEntriesPageAsync(new() { From = BusinessClock.Today, To = BusinessClock.Today, PageSize = 5 });
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(125, page.TotalCount);
        Assert.Equal(125m, page.TotalInflow);
        var second = await test.Entries.GetEntriesPageAsync(new() { From = BusinessClock.Today, To = BusinessClock.Today, PageSize = 5, Page = 2 });
        Assert.Empty(page.Items.Select(e => e.Id).Intersect(second.Items.Select(e => e.Id)));
    }

    [Fact]
    public async Task Reports_ReconcileOrderDiscountAndUnitDiscountAcrossAllViews()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var request = TreasuryTests.Checkout(1);
        request.Order.Discount = 3m;
        request.Order.OrderItems.First().UnitDiscount = 1m;
        var order = await test.Orders.CheckoutAsync(request, "u", true);
        var reports = new ReportService(test.Unit);
        var report = await reports.GetComprehensiveReportAsync(BusinessClock.Today, BusinessClock.Today);
        var products = (await reports.GetBestSellingProductsAsync(BusinessClock.Today, BusinessClock.Today)).ToList();
        var categories = (await reports.GetBestSellingCategoriesAsync(BusinessClock.Today, BusinessClock.Today)).ToList();
        Assert.Equal(15m, order.Total);
        Assert.Equal(5m, report.TotalDiscount);
        Assert.Equal(order.Total, report.TotalSales);
        Assert.Equal(report.TotalSales, products.Sum(p => p.TotalRevenue));
        Assert.Equal(report.TotalProfit, categories.Sum(c => c.TotalProfit));
    }

    [Fact]
    public async Task LaterRefund_IsPostedToday_AndDoesNotEraseYesterday()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = await test.Orders.CheckoutAsync(TreasuryTests.Checkout(1), "u", false);
        var yesterday = BusinessClock.Today.AddDays(-1);
        (await test.Db.Orders.SingleAsync()).Date = yesterday.AddHours(12);
        (await test.Db.CashDrawerEntries.SingleAsync()).Date = yesterday.AddHours(12);
        await test.Db.SaveChangesAsync();
        await test.Cancel(order);
        var reports = new ReportService(test.Unit);
        Assert.Equal(20m, (await reports.GetDailySalesAsync(yesterday, yesterday)).Sum(d => d.TotalSales));
        var today = Assert.Single(await reports.GetDailySalesAsync(BusinessClock.Today, BusinessClock.Today));
        Assert.Equal(-20m, today.TotalSales);
        Assert.Equal(20m, today.TotalRefunds);
        Assert.Equal(-12m, today.TotalProfit);
        Assert.Equal(-20m, (await reports.GetSalesByPaymentMethodAsync(BusinessClock.Today, BusinessClock.Today)).Sum(p => p.TotalSales));
    }

    [Fact]
    public async Task HistoricalLabelsSurviveCatalogChanges_AndPaymentRemoval()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = await test.Orders.CheckoutAsync(TreasuryTests.Checkout(1), "u", false);
        (await test.Db.Products.SingleAsync()).Name = "Changed";
        (await test.Db.Categories.SingleAsync()).Name = "Changed category";
        await test.Db.SaveChangesAsync();
        Assert.True(await new PaymentMethodService(test.Unit, test.Mapper).DeletePaymentMethodAsync(1));
        Assert.False((await test.Db.PaymentMethods.SingleAsync(p => p.Id == 1)).IsActive);
        var read = await test.Orders.GetOrderByIdAsync(order.Id);
        Assert.Equal("Meal", Assert.Single(read!.OrderItems).ProductName);
        Assert.Equal("Cash", Assert.Single(read.CashDrawerEntries).PaymentMethodName);
        var reports = new ReportService(test.Unit);
        Assert.Equal("Food", Assert.Single(await reports.GetBestSellingCategoriesAsync(BusinessClock.Today, BusinessClock.Today)).Name);
        Assert.Equal(20m, Assert.Single(await reports.GetSalesByPaymentMethodAsync(BusinessClock.Today, BusinessClock.Today)).TotalSales);
    }

    [Fact]
    public async Task CategoryDeletionIsRejected_AndUnavailableProductCannotBeSold()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CategoryService(test.Unit, test.Mapper).DeleteCategoryAsync(1));
        Assert.Equal(1, await test.Db.Products.CountAsync());
        (await test.Db.Products.SingleAsync()).IsAvailable = false;
        await test.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => test.Orders.CheckoutAsync(TreasuryTests.Checkout(1), "u", false));
        Assert.Equal(0, await test.Db.Orders.CountAsync());
    }

    [Fact]
    public async Task RequestKeyIsPerUser_AndChangedPayloadIsRejected()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var request = TreasuryTests.Checkout(1);
        await test.Orders.CheckoutAsync(request, "u", false);
        request.PaymentMethodId = 2;
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Orders.CheckoutAsync(request, "u", false));
        request.PaymentMethodId = 1;
        test.Db.Users.Add(new ApplicationUser { Id = "other", UserName = "other" });
        await test.Db.SaveChangesAsync();
        var second = await test.Orders.CheckoutAsync(request, "other", false);
        Assert.NotNull(await test.Orders.GetByRequestAsync(request.ClientRequestId, "other"));
        Assert.Equal(second.Id, (await test.Orders.GetByRequestAsync(request.ClientRequestId, "other"))!.Id);
        Assert.Equal(2, await test.Db.Orders.CountAsync());
    }

    [Fact]
    public async Task StaleQuotedTotalRejectsPaymentWithoutWriting()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var request = TreasuryTests.Checkout(1);
        request.ExpectedTotal = 19m;
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Orders.CheckoutAsync(request, "u", false));
        Assert.Equal(0, await test.Db.Orders.CountAsync());
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task InvalidEnumsAndFractionalCentDiscountNeverWrite()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var request = TreasuryTests.Checkout(1);
        request.Order.OrderType = (OrderType)999;
        await Assert.ThrowsAsync<ArgumentException>(() => test.Orders.CheckoutAsync(request, "u", true));
        request.Order.OrderType = OrderType.Indoor;
        request.Order.OrderItems.First().UnitDiscount = 0.005m;
        await Assert.ThrowsAsync<ArgumentException>(() => test.Orders.CheckoutAsync(request, "u", true));
        Assert.Equal(0, await test.Db.Orders.CountAsync());
    }

    [Fact]
    public void MoneyContractsAndReportBoundaryRejectUnrepresentableValues()
    {
        var dto = new ProductCreateDto { Name = "Meal", CategoryId = 1, SalePrice = 1.005m, CostPrice = 1 };
        Assert.False(Validator.TryValidateObject(dto, new(dto), [], true));
        dto.SalePrice = 10000000000000000m;
        Assert.False(Validator.TryValidateObject(dto, new(dto), [], true));
        Assert.Throws<ArgumentException>(() => ReportPeriod.Create(BusinessClock.Today, DateTime.MaxValue.Date));
    }

    [Fact]
    public void AllocatedDiscountAlwaysReconcilesAtOneCentBoundaries()
    {
        var order = new Order { Discount = 0.01m, OrderItems = Enumerable.Range(0, 3)
            .Select(_ => new OrderItem { Quantity = 1, UnitSalePrice = 0.01m }).ToList() };
        OrderAccounting.Recalculate(order);
        Assert.Equal(0.01m, order.OrderItems.Sum(i => i.OrderDiscountShare));
        Assert.All(order.OrderItems, i => Assert.InRange(i.OrderDiscountShare, 0, i.Total));
        Assert.Equal(order.Total, order.OrderItems.Sum(i => i.Total - i.OrderDiscountShare));
    }
}
