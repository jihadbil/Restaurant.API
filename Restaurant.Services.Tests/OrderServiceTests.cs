using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Restaurant.DataAccess;
using Restaurant.DataAccess.Repositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Xunit;

namespace Restaurant.Services.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task Checkout_UsesServerPrices_AndIsIdempotent()
    {
        await using var testContext = await CreateContextAsync();
        var service = CreateService(testContext.Db);
        var request = CreateCheckoutRequest();

        var first = await service.CheckoutAsync(request, "user-1", canApplyDiscount: false);
        var second = await service.CheckoutAsync(request, "user-1", canApplyDiscount: false);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(20m, first.Total);
        Assert.Equal(8m, first.Cost);
        Assert.Equal(12m, first.Profit);
        Assert.Equal(10m, first.OrderItems.Single().UnitSalePrice);
        Assert.Equal(4m, first.OrderItems.Single().UnitCostPrice);
        Assert.Equal(1, await testContext.Db.Orders.CountAsync());
        Assert.Equal(1, await testContext.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task Checkout_RejectsDiscountWithoutPermission_WithoutWritingAnything()
    {
        await using var testContext = await CreateContextAsync();
        var service = CreateService(testContext.Db);
        var request = CreateCheckoutRequest();
        request.Order.Discount = 1m;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CheckoutAsync(request, "user-1", canApplyDiscount: false));

        Assert.Equal(0, await testContext.Db.Orders.CountAsync());
        Assert.Equal(0, await testContext.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task CancellingPaidOrder_CreatesRefundAndRestoresCashboxBalance()
    {
        await using var testContext = await CreateContextAsync();
        var unitOfWork = new UnitOfWork(testContext.Db);
        var service = new OrderService(unitOfWork, CreateMapper());
        var created = await service.CheckoutAsync(
            CreateCheckoutRequest(),
            "user-1",
            canApplyDiscount: false);

        var updated = await service.UpdateOrderAsync(
            new OrderUpdateDto
            {
                Id = created.Id,
                OrderStatus = OrderStatus.Cancelled,
                OrderType = created.OrderType,
                Notes = created.Notes,
                Discount = created.Discount
            },
            "user-1",
            canApplyDiscount: false,
            canVoidOrder: true);

        var balance = await new CashboxService(unitOfWork, CreateMapper()).GetCashboxBalanceAsync(1);
        Assert.True(updated);
        Assert.Equal(1, await testContext.Db.CashDrawerEntries.CountAsync(e =>
            e.EntryType == CashDrawerEntryType.Refund));
        Assert.NotNull(balance);
        Assert.Equal(100m, balance.CurrentBalance);
    }

    [Fact]
    public async Task ManualCashEntryEndpoint_CannotCreateSalePayment()
    {
        await using var testContext = await CreateContextAsync();
        var service = new CashDrawerEntryService(new UnitOfWork(testContext.Db), CreateMapper());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEntryAsync(
            new CashDrawerEntryCreateDto
            {
                CashboxId = 1,
                Amount = 20m,
                EntryType = CashDrawerEntryType.SalePayment,
                UserId = "forged-user"
            },
            "user-1"));

        Assert.Equal(0, await testContext.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task PaidOrderItems_CannotBeChanged()
    {
        await using var testContext = await CreateContextAsync();
        var unitOfWork = new UnitOfWork(testContext.Db);
        var orderService = new OrderService(unitOfWork, CreateMapper());
        var created = await orderService.CheckoutAsync(
            CreateCheckoutRequest(),
            "user-1",
            canApplyDiscount: false);

        var item = created.OrderItems.Single();
        var itemService = new OrderItemService(unitOfWork, CreateMapper());
        await Assert.ThrowsAsync<InvalidOperationException>(() => itemService.UpdateOrderItemAsync(
            new OrderItemUpdateDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Quantity = 99,
                UnitSalePrice = 0.01m,
                UnitCostPrice = 999m,
                UnitDiscount = 0
            }));

        Assert.Equal(2, (await testContext.Db.OrderItems.SingleAsync()).Quantity);
    }

    private static OrderService CreateService(RestaurantDbContext db)
    {
        return new OrderService(new UnitOfWork(db), CreateMapper());
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            config => config.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    private static OrderCheckoutCreateDto CreateCheckoutRequest()
    {
        return new OrderCheckoutCreateDto
        {
            CashboxId = 1,
            PaymentMethodId = 1,
            ClientRequestId = Guid.NewGuid().ToString("N"),
            Order = new OrderCreateDto
            {
                OrderNumber = 1234,
                UserId = "forged-user",
                OrderType = OrderType.Indoor,
                OrderItems =
                [
                    new OrderItemCreateDto
                    {
                        ProductId = 1,
                        Quantity = 2,
                        UnitSalePrice = 0.01m,
                        UnitCostPrice = 999m
                    }
                ]
            }
        };
    }

    private static async Task<TestDatabase> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new RestaurantDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var category = new Category { Id = 1, Name = "Main" };
        db.Users.Add(new ApplicationUser { Id = "user-1", UserName = "cashier" });
        db.Categories.Add(category);
        db.Products.Add(new Product
        {
            Id = 1,
            Name = "Meal",
            SalePrice = 10m,
            CostPrice = 4m,
            CategoryId = category.Id
        });
        db.Cashboxes.Add(new Cashbox { Id = 1, Name = "Main", InitialBalance = 100m, IsActive = true });
        db.PaymentMethods.Add(new PaymentMethod { Id = 1, Name = "Cash" });
        await db.SaveChangesAsync();

        return new TestDatabase(db);
    }

    private sealed class TestDatabase(RestaurantDbContext db) : IAsyncDisposable
    {
        public RestaurantDbContext Db { get; } = db;

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
        }
    }
}
