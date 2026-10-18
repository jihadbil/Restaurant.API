using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Restaurant.DataAccess;
using Restaurant.DataAccess.Repositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services;
using System.Text.Json;
using Xunit;

namespace Restaurant.Services.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESTAURANT_TEST_SQLSERVER")))
            Skip = "Set RESTAURANT_TEST_SQLSERVER to a test SQL Server with permission to create temporary databases.";
    }
}

public class SqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task MigrationsBootstrapAndConcurrentLedgerOperationsRemainConsistent()
    {
var created = new List<string>();
var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
string Connection(string database)
{
    var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RESTAURANT_TEST_SQLSERVER"))
        { InitialCatalog = database, ConnectTimeout = 5 };
    return connection.ConnectionString;
}
RestaurantDbContext Db(string name) => new(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(Connection(name),
    o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(1), [1205])).Options);
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine(JsonSerializer.Serialize(new { check = name, passed = true })); }
async Task<string> CreateDatabase(string suffix)
{
    var name = "RestaurantAuditFixes_" + Guid.NewGuid().ToString("N") + "_" + suffix;
    await using var connection = new SqlConnection(Connection("master")); await connection.OpenAsync();
    await using var command = new SqlCommand($"CREATE DATABASE [{name}]", connection); await command.ExecuteNonQueryAsync();
    created.Add(name); return name;
}
try
{
    var legacy = await CreateDatabase("Legacy");
    await using (var db = Db(legacy))
    {
        await db.GetService<IMigrator>().MigrateAsync("20261009121534_FixCashLedgerAndShiftClosures");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO AspNetUsers (Id,UserName,NormalizedUserName,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount)
            VALUES ('legacy','legacy','LEGACY',0,0,0,0,0);
            SET IDENTITY_INSERT Categories ON; INSERT INTO Categories(Id,Name) VALUES (1,N'Legacy category'); SET IDENTITY_INSERT Categories OFF;
            SET IDENTITY_INSERT Products ON; INSERT INTO Products(Id,Name,CostPrice,SalePrice,CategoryId) VALUES (1,N'Legacy meal',0.01,0.01,1); SET IDENTITY_INSERT Products OFF;
            SET IDENTITY_INSERT PaymentMethods ON; INSERT INTO PaymentMethods(Id,Name,IsTaxFree,IsCash) VALUES(1,N'Cash',0,1); SET IDENTITY_INSERT PaymentMethods OFF;
            SET IDENTITY_INSERT Cashboxes ON; INSERT INTO Cashboxes(Id,Name,InitialBalance,IsActive,IsDrawer) VALUES(1,N'Drawer',100,1,1); SET IDENTITY_INSERT Cashboxes OFF;
            SET IDENTITY_INSERT Orders ON; INSERT INTO Orders(Id,OrderNumber,Total,Cost,Profit,Discount,Date,OrderStatus,OrderType,Notes,UserId)
            VALUES (1,12345,0.02,0.03,-0.01,0.01,{0},0,0,N'legacy','legacy'); SET IDENTITY_INSERT Orders OFF;
            INSERT INTO OrderItems(Quantity,UnitSalePrice,UnitCostPrice,UnitDiscount,Total,Notes,OrderId,ProductId)
            VALUES(1,0.01,0.01,0,0.01,N'',1,1),(1,0.01,0.01,0,0.01,N'',1,1),(1,0.01,0.01,0,0.01,N'',1,1);
            INSERT INTO CashDrawerEntries(Date,Amount,EntryType,CashboxId,PaymentMethodId,OrderId,UserId,AffectsCash)
            VALUES({0},0.02,1,1,1,1,'legacy',1);
            """, BusinessClock.Today.AddDays(-1).AddHours(12));
        await db.Database.MigrateAsync();
        var items = await db.OrderItems.ToListAsync();
        Check(items.All(i => i.ProductNameSnapshot == "Legacy meal" && i.CategoryNameSnapshot == "Legacy category"), "SQL Server migration backfills historical labels");
        Check(items.Sum(i => i.OrderDiscountShare) == 0.01m, "SQL Server migration reconciles one-cent discount allocation");
        var reports = new ReportService(new UnitOfWork(db));
        var product = (await reports.GetBestSellingProductsAsync(BusinessClock.Today.AddDays(-1), BusinessClock.Today)).Single();
        Check(product.TotalRevenue == 0.02m && product.TotalProfit == -0.01m, "SQL Server translates financial grouping and net allocations");
        var daily = (await reports.GetDailySalesAsync(BusinessClock.Today.AddDays(-1), BusinessClock.Today)).Single();
        Check(daily.TotalSales == 0.02m && daily.TotalCost == 0.03m, "SQL Server translates dated ledger totals");
        await new OrderService(new UnitOfWork(db), mapper).UpdateOrderAsync(new OrderUpdateDto {
            Id = 1, OrderStatus = OrderStatus.Cancelled, Discount = 0.01m }, "legacy", true, true);
        Check((await reports.GetDailySalesAsync(BusinessClock.Today.AddDays(-1), BusinessClock.Today.AddDays(-1))).Single().TotalSales == 0.02m,
            "SQL Server refund does not rewrite previous-day revenue");
        await db.GetService<IMigrator>().MigrateAsync("20260711014234_AddPaymentMethodLogoAndRestaurantInfo");
        await using var readConnection = new SqlConnection(Connection(legacy)); await readConnection.OpenAsync();
        await using var readRefunds = new SqlCommand("SELECT COUNT(*) FROM CashDrawerEntries WHERE EntryType = 4", readConnection);
        Check((int)(await readRefunds.ExecuteScalarAsync())! == 1, "SQL Server downgrade retains posted refunds");
    }

    var fresh = await CreateDatabase("Fresh");
    var services = new ServiceCollection(); services.AddLogging(); services.AddDataProtection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["BootstrapAdmin:UserName"] = "verification-admin", ["BootstrapAdmin:Email"] = "verification@example.invalid", ["BootstrapAdmin:Password"] = "Verification@123456" }).Build());
    services.AddDbContext<RestaurantDbContext>(o => o.UseSqlServer(Connection(fresh)));
    services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<RestaurantDbContext>().AddDefaultTokenProviders();
    services.AddScoped<DataSeeder>(); await using var provider = services.BuildServiceProvider();
    await using (var scope = provider.CreateAsyncScope()) { await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(); }
    string userId; int drawerId, targetId, productId;
    await using (var db = Db(fresh))
    {
        Check(!(await db.PaymentMethods.SingleAsync(p => p.Name == "بطاقة")).IsCash, "fresh SQL Server bootstrap classifies card as noncash");
        userId = (await db.Users.SingleAsync()).Id; targetId = (await db.Cashboxes.SingleAsync()).Id;
        var drawer = new Cashbox { Name = "Verification drawer", IsDrawer = true, InitialBalance = 100 };
        db.Cashboxes.Add(drawer); await db.SaveChangesAsync(); drawerId = drawer.Id;
        productId = (await db.Products.OrderBy(p => p.Id).FirstAsync()).Id;
        Check(!db.Database.HasPendingModelChanges(), "SQL Server model matches migration snapshot");
    }
    var request = new OrderCheckoutCreateDto { CashboxId = drawerId, PaymentMethodId = 1, ClientRequestId = Guid.NewGuid().ToString("N"),
        Order = new() { OrderNumber = 12345, UserId = userId, Discount = 3, OrderItems = [new() { ProductId = productId, Quantity = 2 }] } };
    var attempts = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
    {
        await using var db = Db(fresh); return await new OrderService(new UnitOfWork(db), mapper).CheckoutAsync(request, userId, true);
    }));
    Check(attempts.Select(o => o.Id).Distinct().Count() == 1, "six concurrent SQL Server checkout retries return one sale");
    var sale = attempts[0];
    var withdrawals = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
    {
        await using var db = Db(fresh);
        try { await new CashDrawerEntryService(new UnitOfWork(db), mapper).CreateEntryAsync(new() {
            CashboxId = drawerId, Amount = 80, EntryType = CashDrawerEntryType.Outflow, ClientRequestId = Guid.NewGuid().ToString("N") }, userId); return true; }
        catch (InvalidOperationException) { return false; }
    }));
    Check(withdrawals.Count(v => v) == 1, "concurrent SQL Server withdrawals cannot overdraw the drawer");
    CashShiftContextDto boundary; decimal actual;
    await using (var db = Db(fresh))
    {
        boundary = await new CashShiftService(new UnitOfWork(db)).GetContextAsync(drawerId, true);
        actual = boundary.ExpectedCash!.Value;
        Check(await db.Orders.CountAsync() == 1 && await db.CashDrawerEntries.CountAsync(e => e.EntryType == CashDrawerEntryType.SalePayment) == 1,
            "SQL Server ledger contains exactly one checkout payment");
    }
    var closures = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
    {
        await using var db = Db(fresh);
        try { await new CashShiftService(new UnitOfWork(db)).CloseAsync(new() { SourceCashboxId = drawerId, TargetCashboxId = targetId,
            ActualCash = actual, PreviousShiftId = boundary.PreviousShiftId, LastEntryId = boundary.LastEntryId,
            ClientRequestId = Guid.NewGuid().ToString("N") }, userId, true); return true; }
        catch (InvalidOperationException) { return false; }
    }));
    Check(closures.Count(v => v) == 1, "concurrent SQL Server close requests create one closure");
    await using (var db = Db(fresh))
    {
        var unit = new UnitOfWork(db);
        Check((await new CashboxService(unit, mapper).GetCashboxBalanceAsync(drawerId))!.CurrentBalance == 0, "closed SQL Server drawer reconciles to zero");
        var reports = await new ReportService(unit).GetComprehensiveReportAsync(BusinessClock.Today, BusinessClock.Today);
        Check(reports.TotalSales == sale.Total && reports.TotalProfit == sale.Profit, "SQL Server comprehensive report reconciles to posted sale");
        var category = new Category { Name = "Delete test" }; db.Categories.Add(category); await db.SaveChangesAsync();
        db.Products.Add(new Product { Name = "Must survive", CategoryId = category.Id, SalePrice = 1, CostPrice = 1 }); await db.SaveChangesAsync();
        try { await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Categories WHERE Id = {category.Id}"); throw new Exception("FK allowed cascade"); }
        catch (SqlException error) when (error.Number == 547) { db.ChangeTracker.Clear(); }
        Check(await db.Products.AnyAsync(p => p.Name == "Must survive"), "SQL Server FK blocks category cascade deletion");
    }
}
finally
{
    SqlConnection.ClearAllPools();
    await using var connection = new SqlConnection(Connection("master")); await connection.OpenAsync();
    foreach (var name in created)
    {
        if (!name.StartsWith("RestaurantAuditFixes_", StringComparison.Ordinal) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^RestaurantAuditFixes_[a-f0-9]{32}_(Legacy|Fresh)$")) throw new Exception("Unsafe cleanup target");
        await using var command = new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];", connection);
        await command.ExecuteNonQueryAsync(); Console.WriteLine(JsonSerializer.Serialize(new { cleaned = name }));
    }
}

    }
}
