using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.DataAccess;
using Restaurant.DataAccess.Repositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Restaurant.Services.Tests;

public class TreasuryTests
{
    [Fact]
    public void WebPayload_ValidatesWithoutUserId()
    {
        var dto = new CashDrawerEntryCreateDto { CashboxId = 1, Amount = 10m, EntryType = CashDrawerEntryType.Inflow };
        Assert.True(Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true));
    }

    [Theory]
    [InlineData(0)] [InlineData(-10)] [InlineData(0.001)]
    public async Task InvalidManualAmount_WritesNothing(decimal amount)
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => test.Entries.CreateEntryAsync(Entry(amount), "u"));
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task ManualEntry_UsesAuthenticatedUser_AndRetriesDoNotDuplicate()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var dto = Entry(10);
        dto.UserId = "forged-user";
        var first = await test.Entries.CreateEntryAsync(dto, "u");
        var second = await test.Entries.CreateEntryAsync(dto, "u");
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("u", first.UserId);
        Assert.Equal(110m, await test.Balance(1));
        dto.Amount = 11;
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Entries.CreateEntryAsync(dto, "u"));
    }

    [Fact]
    public async Task Overdraw_IsRejectedWithoutChangingBalance()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Entries.CreateEntryAsync(Entry(101, CashDrawerEntryType.Outflow), "u"));
        Assert.Equal(100m, await test.Balance(1));
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
    }

    [Theory]
    [InlineData(1)] [InlineData(3)] [InlineData(4)] [InlineData(999)]
    public async Task InvalidTransferTarget_WritesNeitherLegNorClosure(int target)
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var dto = await test.CloseRequest(100);
        dto.TargetCashboxId = target;
        await Assert.ThrowsAsync<ArgumentException>(() => test.Shifts.CloseAsync(dto, "u", true));
        Assert.Equal(0, await test.Db.CashShifts.CountAsync());
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
        Assert.Equal(100m, await test.Balance(1));
    }

    [Theory]
    [InlineData(90, -10, 1090)] [InlineData(110, 10, 1110)] [InlineData(100, 0, 1100)] [InlineData(0, -100, 1000)]
    public async Task Close_RecordsDifference_EmptiesDrawer_AndCreditsActualCash(decimal actual, decimal difference, decimal target)
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var dto = await test.CloseRequest(actual);
        var saved = await test.Shifts.CloseAsync(dto, "u", true);
        Assert.Equal(100m, saved.ExpectedCash);
        Assert.Equal(difference, saved.Difference);
        Assert.Equal(0m, await test.Balance(1));
        Assert.Equal(target, await test.Balance(2));
        Assert.Equal(1, await test.Db.CashShifts.CountAsync());
        var repeated = await test.Shifts.CloseAsync(dto, "u", true);
        Assert.Equal(saved.Id, repeated.Id);
        Assert.Equal(target, await test.Balance(2));
        var context = await test.Shifts.GetContextAsync(1, true);
        Assert.False(context.CanClose);
        var duplicate = await test.CloseRequest(0);
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Shifts.CloseAsync(duplicate, "u", true));
    }

    [Fact]
    public async Task FailureAfterSavingTransfer_RollsBackBothLegsAndClosure()
    {
        var failure = new FailSecondSave();
        await using var test = await TreasuryDatabase.CreateAsync(failure);
        var request = await test.CloseRequest(90);
        failure.Armed = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Shifts.CloseAsync(request, "u", true));
        Assert.Equal(0, await test.Db.CashShifts.CountAsync());
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
        Assert.Equal(100m, await test.Balance(1));
        Assert.Equal(1000m, await test.Balance(2));
    }

    [Fact]
    public async Task StaleCountBoundary_IsRejected_ThenNewPeriodStartsAfterClosure()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var stale = await test.CloseRequest(100);
        await test.Entries.CreateEntryAsync(Entry(10), "u");
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Shifts.CloseAsync(stale, "u", true));
        var saved = await test.Shifts.CloseAsync(await test.CloseRequest(110), "u", true);
        await test.Entries.CreateEntryAsync(Entry(15), "u");
        var newContext = await test.Shifts.GetContextAsync(1, true);
        Assert.True(newContext.CanClose);
        Assert.Equal(saved.Id, newContext.PreviousShiftId);
        Assert.Equal(15m, newContext.TotalManualInflow);
        Assert.Equal(15m, newContext.ExpectedCash);
    }

    [Fact]
    public async Task BlindClose_NeverReturnsExpectedCashOrDifference()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var context = await test.Shifts.GetContextAsync(1, false);
        Assert.Null(context.ExpectedCash);
        Assert.Null(context.TotalSales);
        Assert.Null(context.TotalOutflow);
        Assert.Null(context.PaymentSummary);
        var saved = await test.Shifts.CloseAsync(await test.CloseRequest(90), "u", false);
        Assert.Null(saved.ExpectedCash);
        Assert.Null(saved.Difference);
        Assert.Equal(-10m, (await test.Db.CashShifts.SingleAsync()).Difference);
    }

    [Fact]
    public async Task ClosedPeriodAndTransferEntries_CannotBeDeleted()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var entry = await test.Entries.CreateEntryAsync(Entry(10), "u");
        await test.Shifts.CloseAsync(await test.CloseRequest(110), "u", true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Entries.DeleteEntryAsync(entry.Id));
        var transfer = await test.Db.CashDrawerEntries.FirstAsync(e => e.EntryType == CashDrawerEntryType.TransferIn);
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Entries.DeleteEntryAsync(transfer.Id));
    }

    [Fact]
    public async Task CardSaleAndRefund_StayOutsideCashBalance_EvenIfPaymentMethodChanges()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = await test.Orders.CheckoutAsync(Checkout(2), "u", false);
        Assert.Equal(100m, await test.Balance(1));
        var method = await test.Db.PaymentMethods.SingleAsync(p => p.Id == 2);
        method.IsCash = true;
        await test.Db.SaveChangesAsync();
        await test.Cancel(order);
        Assert.Equal(100m, await test.Balance(1));
        Assert.All(await test.Db.CashDrawerEntries.ToListAsync(), e => Assert.False(e.AffectsCash));
    }

    [Fact]
    public async Task CashRefundAfterShiftClose_RequiresDrawerFunding_AndDoesNotCancelOnFailure()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = await test.Orders.CheckoutAsync(Checkout(1), "u", false);
        await test.Shifts.CloseAsync(await test.CloseRequest(120), "u", true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Cancel(order));
        Assert.NotEqual(OrderStatus.Cancelled, (await test.Db.Orders.SingleAsync()).OrderStatus);
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync(e => e.EntryType == CashDrawerEntryType.Refund));
        await test.Entries.CreateEntryAsync(Entry(20), "u");
        await test.Cancel(order);
        Assert.Equal(0m, await test.Balance(1));
    }

    [Fact]
    public async Task DateOnlyEndFilter_IncludesEntireDay()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var entry = await test.Entries.CreateEntryAsync(Entry(10), "u");
        var stored = await test.Db.CashDrawerEntries.SingleAsync();
        stored.Date = new DateTime(2026, 10, 9, 23, 59, 59).AddMilliseconds(900);
        await test.Db.SaveChangesAsync();
        var entries = await test.Entries.GetAllEntriesAsync(1, new DateTime(2026, 10, 9), new DateTime(2026, 10, 9));
        Assert.Equal(entry.Id, Assert.Single(entries).Id);
    }

    [Fact]
    public async Task CashMethodChangeOrDeletion_DoesNotRewriteHistoricalBalance()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = await test.Orders.CheckoutAsync(Checkout(1), "u", false);
        var method = await test.Db.PaymentMethods.SingleAsync(p => p.Id == 1);
        method.IsCash = false;
        await test.Db.SaveChangesAsync();
        Assert.Equal(120m, await test.Balance(1));
        test.Db.PaymentMethods.Remove(method);
        await test.Db.SaveChangesAsync();
        await test.Cancel(order);
        Assert.Equal(100m, await test.Balance(1));
        Assert.All(await test.Db.CashDrawerEntries.ToListAsync(), e => Assert.True(e.AffectsCash));
    }

    [Fact]
    public async Task CheckoutIntoMainTreasury_IsRejected()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var request = Checkout(1);
        request.CashboxId = 2;
        await Assert.ThrowsAsync<ArgumentException>(() => test.Orders.CheckoutAsync(request, "u", false));
        Assert.Equal(0, await test.Db.Orders.CountAsync());
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task ZeroClosureWithoutEntries_StillProtectsCashboxesFromDeletion()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        (await test.Db.Cashboxes.SingleAsync(c => c.Id == 1)).InitialBalance = 0;
        await test.Db.SaveChangesAsync();
        await test.Shifts.CloseAsync(await test.CloseRequest(0), "u", true);
        Assert.Equal(0, await test.Db.CashDrawerEntries.CountAsync());
        var boxes = new CashboxService(test.Unit, test.Mapper);
        Assert.False(await boxes.DeleteCashboxAsync(1));
        Assert.False(await boxes.DeleteCashboxAsync(2));
    }

    [Fact]
    public async Task ZeroClosureWithoutEntries_StillProtectsCashierHistoryFromDeletion()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        (await test.Db.Cashboxes.SingleAsync(c => c.Id == 1)).InitialBalance = 0;
        await test.Db.SaveChangesAsync();
        await test.Shifts.CloseAsync(await test.CloseRequest(0), "u", true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<RestaurantDbContext>(o => o.UseSqlite(test.Connection));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<RestaurantDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = new UserService(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(), test.Mapper,
            new UnitOfWork(scope.ServiceProvider.GetRequiredService<RestaurantDbContext>()));
        Assert.False(await users.DeleteUserAsync("u"));
        Assert.Equal(1, await test.Db.Users.CountAsync());
    }

    internal static CashDrawerEntryCreateDto Entry(decimal amount, CashDrawerEntryType type = CashDrawerEntryType.Inflow) =>
        new() { CashboxId = 1, Amount = amount, EntryType = type, ClientRequestId = Guid.NewGuid().ToString("N") };
    internal static OrderCheckoutCreateDto Checkout(int payment) => new()
    {
        CashboxId = 1, PaymentMethodId = payment, ClientRequestId = Guid.NewGuid().ToString("N"),
        Order = new OrderCreateDto { UserId = "u", OrderType = OrderType.Indoor,
            OrderItems = [new OrderItemCreateDto { ProductId = 1, Quantity = 2 }] }
    };

    private class FailSecondSave : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        private int _calls;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Armed && ++_calls == 2) throw new InvalidOperationException("Injected failure after transfer save");
            return ValueTask.FromResult(result);
        }
    }
}

internal sealed class TreasuryDatabase : IAsyncDisposable
{
    internal SqliteConnection Connection { get; }
    internal RestaurantDbContext Db { get; }
    internal IMapper Mapper { get; } = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
    internal UnitOfWork Unit { get; }
    internal CashDrawerEntryService Entries => new(Unit, Mapper);
    internal CashShiftService Shifts => new(Unit);
    internal OrderService Orders => new(Unit, Mapper);
    private TreasuryDatabase(SqliteConnection connection, RestaurantDbContext db) { Connection = connection; Db = db; Unit = new(db); }
    internal static async Task<TreasuryDatabase> CreateAsync(IInterceptor? interceptor = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlite(connection);
        if (interceptor != null) options.AddInterceptors(interceptor);
        var db = new RestaurantDbContext(options.Options);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new ApplicationUser { Id = "u", UserName = "cashier" });
        db.Cashboxes.AddRange(new Cashbox { Id = 1, Name = "Drawer", IsDrawer = true, IsActive = true, InitialBalance = 100m },
            new Cashbox { Id = 2, Name = "Treasury", InitialBalance = 1000m, IsActive = true },
            new Cashbox { Id = 3, Name = "Other Drawer", IsDrawer = true, IsActive = true },
            new Cashbox { Id = 4, Name = "Inactive", IsActive = false });
        db.PaymentMethods.AddRange(new PaymentMethod { Id = 1, Name = "Cash", IsCash = true }, new PaymentMethod { Id = 2, Name = "Card", IsCash = false });
        db.Categories.Add(new Category { Id = 1, Name = "Food" });
        db.Products.Add(new Product { Id = 1, Name = "Meal", CategoryId = 1, SalePrice = 10m, CostPrice = 4m });
        await db.SaveChangesAsync();
        return new(connection, db);
    }
    internal async Task<decimal> Balance(int id) => (await new CashboxService(Unit, Mapper).GetCashboxBalanceAsync(id))!.CurrentBalance;
    internal async Task<CashShiftCloseDto> CloseRequest(decimal actual)
    {
        var ctx = await Shifts.GetContextAsync(1, true);
        return new() { SourceCashboxId = 1, TargetCashboxId = 2, ActualCash = actual,
            PreviousShiftId = ctx.PreviousShiftId, LastEntryId = ctx.LastEntryId, ClientRequestId = Guid.NewGuid().ToString("N") };
    }
    internal Task<bool> Cancel(OrderDto order) => Orders.UpdateOrderAsync(new OrderUpdateDto
        { Id = order.Id, OrderStatus = OrderStatus.Cancelled, OrderType = order.OrderType, Discount = order.Discount }, "u", false, true);
    public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }
}
