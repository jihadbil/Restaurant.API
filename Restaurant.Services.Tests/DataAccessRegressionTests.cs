using System.Data.Common;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
using Xunit;

namespace Restaurant.Services.Tests;

public class DataAccessRegressionTests
{
    private const string PreviousMigration = "20261017070959_EnforceUniquePrinterConfigurations";

    [SqlServerTheory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task ConcurrentItemWritesKeepPersistedOrderTotalsConsistent(string secondOperation)
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        int orderId, itemId, productId;
        await using (var setup = database.Context())
        {
            setup.Users.Add(new ApplicationUser { Id = "u", UserName = "cashier" });
            var category = new Category { Name = "Concurrency category" };
            var product = new Product { Name = "Concurrency meal", Category = category, SalePrice = 5, CostPrice = 2 };
            setup.Products.Add(product);
            var item = new OrderItem { Product = product, Quantity = 1, UnitSalePrice = 5, UnitCostPrice = 2, Total = 5 };
            var order = new Order { UserId = "u", OrderNumber = 12345, Date = BusinessClock.Now,
                OrderStatus = OrderStatus.Preparing, OrderType = OrderType.Indoor, OrderItems = [item] };
            OrderAccounting.Recalculate(order);
            setup.Orders.Add(order);
            await setup.SaveChangesAsync();
            (orderId, itemId, productId) = (order.Id, item.Id, product.Id);
        }
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var releaseWrites = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstWrite = new BeforeOrderWrite(releaseWrites.Task); var secondWrite = new BeforeOrderWrite(releaseWrites.Task);
        await using var firstDb = database.Context(firstWrite);
        await using var secondDb = database.Context(secondWrite);
        var firstService = new OrderItemService(new UnitOfWork(firstDb), mapper);
        var secondService = new OrderItemService(new UnitOfWork(secondDb), mapper);
        var first = firstService.CreateOrderItemAsync(orderId, new() { ProductId = productId, Quantity = 1 });
        Task second = secondOperation switch
        {
            "add" => secondService.CreateOrderItemAsync(orderId, new() { ProductId = productId, Quantity = 1 }),
            "update" => secondService.UpdateOrderItemAsync(new() { Id = itemId, ProductId = productId, Quantity = 4 }),
            _ => secondService.DeleteOrderItemAsync(itemId)
        };
        try
        {
            await Task.WhenAll(firstWrite.Ready.Task, secondWrite.Ready.Task).WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            releaseWrites.TrySetResult();
            await Task.WhenAll(first, second);
        }
        Assert.NotNull(await first);
        await using var read = database.Context();
        var saved = await read.Orders.Include(order => order.OrderItems).SingleAsync();
        Assert.Equal(saved.OrderItems.Sum(item => item.Total), saved.Total);
        Assert.Equal(saved.OrderItems.Sum(item => item.Quantity * item.UnitCostPrice), saved.Cost);
        Assert.Equal(saved.Total - saved.Cost, saved.Profit);
        Assert.Equal(secondOperation == "delete" ? 1 : secondOperation == "add" ? 3 : 2, saved.OrderItems.Count);
    }

    [SqlServerFact]
    public async Task UniqueEmailMigrationPreservesDuplicatesForReviewThenEnforcesUniqueness()
    {
        const string previous = "20261017080416_HardenDataAccessTransactions";
        await using var database = await SqlDatabase.Create();
        await database.Migrate(previous);
        await using (var db = database.Context())
        {
            db.Users.AddRange(new ApplicationUser { Id = "first", UserName = "first", Email = "shared@example.test", NormalizedEmail = "SHARED@EXAMPLE.TEST" },
                new ApplicationUser { Id = "second", UserName = "second", Email = "shared@example.test", NormalizedEmail = "SHARED@EXAMPLE.TEST" });
            await db.SaveChangesAsync();
        }
        var error = await Assert.ThrowsAsync<SqlException>(() => database.Migrate());
        Assert.Equal(51000, error.Number);
        await using (var db = database.Context())
        {
            Assert.Equal(2, await db.Users.CountAsync());
            var second = await db.Users.SingleAsync(user => user.Id == "second");
            second.Email = "second@example.test"; second.NormalizedEmail = "SECOND@EXAMPLE.TEST";
            await db.SaveChangesAsync();
        }
        await database.Migrate();
        await using (var db = database.Context())
        {
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ApplicationUser { Id = "duplicate", UserName = "duplicate", NormalizedEmail = "SHARED@EXAMPLE.TEST" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await database.Migrate(previous);
        await database.Migrate();
        await using var final = database.Context();
        Assert.Equal(2, await final.Users.CountAsync());
    }

    private sealed class BeforeOrderWrite(Task release) : DbCommandInterceptor
    {
        internal TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private async Task WaitForOtherWriter(DbCommand command, CancellationToken cancellationToken)
        {
            if (command.CommandText.Contains("UPDATE [Orders]", StringComparison.Ordinal)
                || command.CommandText.Contains("INSERT INTO [OrderItems]", StringComparison.Ordinal)
                || command.CommandText.Contains("UPDATE [OrderItems]", StringComparison.Ordinal)
                || command.CommandText.Contains("DELETE FROM [OrderItems]", StringComparison.Ordinal))
            {
                Ready.TrySetResult();
                await release.WaitAsync(cancellationToken);
            }
        }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            await WaitForOtherWriter(command, cancellationToken);
            return result;
        }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await WaitForOtherWriter(command, cancellationToken);
            return result;
        }
    }

    [SqlServerFact]
    public async Task IdempotentMigrationScriptCanBeExecutedTwice()
    {
        await using var database = await SqlDatabase.Create();
        await using var db = database.Context();
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [SqlServerFact]
    public async Task LostCommitAcknowledgementReturnsSavedResultWithoutReplayingTheOperation()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        await using var db = database.Context(new CommitFault(afterCommit: true));
        var unit = new UnitOfWork(db);
        var attempts = 0;
        var id = await unit.ExecuteInTransactionAsync(async () =>
        {
            attempts++;
            var addon = new Addon { Name = "Exactly once" };
            await unit.Addons.AddAsync(addon);
            await unit.SaveAsync();
            return addon.Id;
        });

        Assert.Equal(1, attempts);
        Assert.Equal(id, (await db.Addons.AsNoTracking().SingleAsync()).Id);
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [__TransactionReceipts]").SingleAsync());
    }

    [SqlServerFact]
    public async Task ReceiptCleanupFailureDoesNotTurnACommittedWriteIntoAFailure()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        await using var db = database.Context(new ReceiptCleanupFault());
        var unit = new UnitOfWork(db);
        Assert.True(await unit.ExecuteInTransactionAsync(async () =>
        {
            await unit.Addons.AddAsync(new Addon { Name = "Committed" });
            await unit.SaveAsync();
            return true;
        }));
        Assert.Single(await db.Addons.AsNoTracking().ToListAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [__TransactionReceipts]").SingleAsync());
    }

    [SqlServerFact]
    public async Task FailureBeforeCommitRetriesTheTransactionWithoutRetainingRolledBackChanges()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        await using var db = database.Context(new CommitFault(afterCommit: false));
        var unit = new UnitOfWork(db);
        var attempts = 0;
        await unit.ExecuteInTransactionAsync(async () =>
        {
            attempts++;
            await unit.Addons.AddAsync(new Addon { Name = "Retry" });
            await unit.SaveAsync();
            return true;
        });
        Assert.Equal(2, attempts);
        Assert.Single(await db.Addons.AsNoTracking().ToListAsync());
    }

    [SqlServerFact]
    public async Task RollbackFailureDoesNotHideTheBusinessErrorOrLeaveTrackedChanges()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        await using var db = database.Context(new RollbackFault());
        var unit = new UnitOfWork(db);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => unit.ExecuteInTransactionAsync<bool>(async () =>
        {
            await unit.Addons.AddAsync(new Addon { Name = "Must roll back" });
            await unit.SaveAsync();
            throw new ArgumentException("Original error");
        }));
        Assert.Equal("Original error", error.Message);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(await db.Addons.AsNoTracking().ToListAsync());
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BootstrapRecoversTheLegacyPermanentLockEvenAfterAnInterruptedPasswordReset(bool alreadyReset)
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddLogging(); services.AddDataProtection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<RestaurantDbContext>(o => o.UseSqlServer(database.ConnectionString));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<RestaurantDbContext>().AddDefaultTokenProviders();
        services.AddScoped<DataSeeder>();
        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole(AppRoles.Admin))).Succeeded);
            var admin = new ApplicationUser { UserName = "admin", Email = "legacy@example.invalid" };
            Assert.True((await manager.CreateAsync(admin, "Admin@123456")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(admin, AppRoles.Admin)).Succeeded);
            await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync());
            Assert.True(await manager.IsLockedOutAsync(admin));
            if (alreadyReset)
                Assert.True((await manager.ResetPasswordAsync(admin, await manager.GeneratePasswordResetTokenAsync(admin), "Recovered@123456")).Succeeded);
        }
        configuration["BootstrapAdmin:UserName"] = "admin";
        configuration["BootstrapAdmin:Email"] = "legacy@example.invalid";
        configuration["BootstrapAdmin:Password"] = "Recovered@123456";
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await manager.FindByNameAsync("admin"))!;
            Assert.False(await manager.IsLockedOutAsync(admin));
            Assert.True(await manager.CheckPasswordAsync(admin, "Recovered@123456"));
            // A later temporary lock must not be removed by routine startup.
            Assert.True((await manager.SetLockoutEndDateAsync(admin, DateTimeOffset.UtcNow.AddMinutes(10))).Succeeded);
        }
        await using (var scope = provider.CreateAsyncScope())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync());
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True(await manager.IsLockedOutAsync((await manager.FindByNameAsync("admin"))!));
        }
    }

    [SqlServerFact]
    public async Task LegacyUpgradeHandlesNumberCollisionsIntegerLimitsAndPartialRefunds()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate("20260711014234_AddPaymentMethodLogoAndRestaurantInfo");
        await using var db = database.Context();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO AspNetUsers(Id,UserName,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount)
                VALUES ('u','legacy',0,0,0,0,0);
            SET IDENTITY_INSERT Cashboxes ON;
            INSERT INTO Cashboxes(Id,Name,InitialBalance,IsActive) VALUES (1,N'Drawer',100,1);
            SET IDENTITY_INSERT Cashboxes OFF;
            SET IDENTITY_INSERT Orders ON;
            INSERT INTO Orders(Id,OrderNumber,Total,Cost,Profit,Discount,Date,OrderStatus,Notes,UserId) VALUES
                (1,42,10,0,10,0,GETDATE(),0,N'','u'), (2,42,10,0,10,0,GETDATE(),0,N'','u'),
                (3,1000000002,10,0,10,0,GETDATE(),0,N'','u'),
                (4,2147483647,10,0,10,0,GETDATE(),0,N'','u'), (5,2147483647,10,0,10,0,GETDATE(),0,N'','u'),
                (6,1,10,0,10,0,GETDATE(),0,N'','u'),
                (10,50,20,0,20,0,GETDATE(),3,N'','u'), (11,51,10,0,10,0,GETDATE(),3,N'','u');
            SET IDENTITY_INSERT Orders OFF;
            INSERT INTO CashDrawerEntries(Date,Amount,EntryType,CashboxId,OrderId,UserId) VALUES
                (GETDATE(),10,1,1,10,'u'), (GETDATE(),10,1,1,10,'u'), (GETDATE(),10,4,1,10,'u'),
                (GETDATE(),10,1,1,11,'u'), (GETDATE(),5,4,1,11,'u');
            """);
        await database.Migrate();
        var orders = await db.Orders.AsNoTracking().ToListAsync();
        Assert.Equal(orders.Count, orders.Select(o => o.OrderNumber).Distinct().Count());
        Assert.All(orders, o => Assert.True(o.OrderNumber > 0));
        Assert.Equal(42, orders.Single(o => o.Id == 1).OrderNumber);
        Assert.Equal(int.MaxValue, orders.Single(o => o.Id == 4).OrderNumber);
        Assert.Equal(20, await db.CashDrawerEntries.Where(e => e.OrderId == 10 && e.EntryType == CashDrawerEntryType.Refund).SumAsync(e => e.Amount));
        Assert.Equal(10, await db.CashDrawerEntries.Where(e => e.OrderId == 11 && e.EntryType == CashDrawerEntryType.Refund).SumAsync(e => e.Amount));
    }

    [SqlServerFact]
    public async Task AlreadyAppliedRefundBugsAreRepairedWithAnAuditAndDoNotRepeatOnReupgrade()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate(PreviousMigration);
        await using var db = database.Context();
        db.Users.Add(new ApplicationUser { Id = "u", UserName = "legacy" });
        db.Cashboxes.Add(new Cashbox { Name = "Drawer", IsDrawer = true, InitialBalance = 100 });
        db.Orders.AddRange(Order(1, "u", null, cancelled: true), Order(2, "u", null, cancelled: true));
        await db.SaveChangesAsync();
        db.CashDrawerEntries.AddRange(Entry(1, 1, 10, CashDrawerEntryType.SalePayment),
            Entry(1, 1, 5, CashDrawerEntryType.Refund),
            Entry(1, 1, 10, CashDrawerEntryType.Refund, automatic: true),
            Entry(2, 1, 10, CashDrawerEntryType.SalePayment), Entry(2, 1, 10, CashDrawerEntryType.SalePayment),
            Entry(2, 1, 10, CashDrawerEntryType.Refund));
        await db.SaveChangesAsync();
        await database.Migrate();
        db.ChangeTracker.Clear();
        Assert.Equal(10, await db.CashDrawerEntries.Where(e => e.OrderId == 1 && e.EntryType == CashDrawerEntryType.Refund).SumAsync(e => e.Amount));
        Assert.Equal(20, await db.CashDrawerEntries.Where(e => e.OrderId == 2 && e.EntryType == CashDrawerEntryType.Refund).SumAsync(e => e.Amount));
        Assert.Equal(5, await db.CashDrawerEntries.Where(e => e.OrderId == 1 && e.Notes == null).Select(e => e.Amount).MinAsync());
        var auditCount = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [__LegacyLedgerRepairs]").SingleAsync();
        Assert.Equal(2, auditCount);
        Assert.Equal(10, await db.Database.SqlQueryRaw<decimal>("SELECT [OldAmount] AS [Value] FROM [__LegacyLedgerRepairs] WHERE [Reason] = N'Automatic refund overpayment'").SingleAsync());
        await database.Migrate(PreviousMigration);
        await database.Migrate();
        Assert.Equal(auditCount, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [__LegacyLedgerRepairs]").SingleAsync());
        Assert.Equal(100, CashLedger.Balance(100, await db.CashDrawerEntries.AsNoTracking().ToListAsync()));
    }

    [SqlServerFact]
    public async Task CardRepairPreservesPhysicalClosedBalancesAndDoesNotChangeModernPaymentSnapshots()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate(PreviousMigration);
        await using var db = database.Context();
        db.Users.Add(new ApplicationUser { Id = "u", UserName = "legacy" });
        db.PaymentMethods.Add(new PaymentMethod { Name = "بطاقة", IsCash = false });
        db.Cashboxes.AddRange(new Cashbox { Name = "Open", IsDrawer = true, InitialBalance = 100 },
            new Cashbox { Name = "Closed", IsDrawer = true, InitialBalance = 100 },
            new Cashbox { Name = "Treasury" });
        db.Orders.AddRange(Order(1, "u", null), Order(2, "u", null), Order(3, "u", "modern"));
        await db.SaveChangesAsync();
        var open = Entry(1, 1, 25, CashDrawerEntryType.SalePayment); open.PaymentMethodId = 1;
        var closed = Entry(2, 2, 25, CashDrawerEntryType.SalePayment); closed.PaymentMethodId = 1;
        var modern = Entry(3, 1, 12, CashDrawerEntryType.SalePayment); modern.PaymentMethodId = 1;
        db.CashDrawerEntries.AddRange(open, closed, modern,
            Entry(null, 2, 25, CashDrawerEntryType.CashShortage), Entry(null, 2, 100, CashDrawerEntryType.TransferOut),
            Entry(null, 3, 100, CashDrawerEntryType.TransferIn));
        await db.SaveChangesAsync();
        db.CashShifts.Add(new CashShift { SourceCashboxId = 2, TargetCashboxId = 3, UserId = "u",
            ClientRequestId = "close", LastEntryId = await db.CashDrawerEntries.Where(e => e.CashboxId == 2).MaxAsync(e => e.Id),
            StartedAt = BusinessClock.Now, ClosedAt = BusinessClock.Now, ExpectedCash = 125, ActualCash = 100, Difference = -25 });
        await db.SaveChangesAsync();
        await database.Migrate();
        db.ChangeTracker.Clear();
        var entries = await db.CashDrawerEntries.AsNoTracking().ToListAsync();
        Assert.False(entries.Single(e => e.Id == open.Id).AffectsCash);
        Assert.False(entries.Single(e => e.Id == closed.Id).AffectsCash);
        Assert.True(entries.Single(e => e.Id == modern.Id).AffectsCash);
        Assert.Equal(112, CashLedger.Balance(100, entries.Where(e => e.CashboxId == 1)));
        Assert.Equal(0, CashLedger.Balance(100, entries.Where(e => e.CashboxId == 2)));
        Assert.Equal(100, CashLedger.Balance(0, entries.Where(e => e.CashboxId == 3)));
        Assert.Equal(25, Assert.Single(entries, e => e.EntryType == CashDrawerEntryType.CashSurplus).Amount);
        await database.Migrate(PreviousMigration);
        await database.Migrate();
        Assert.Single(await db.CashDrawerEntries.Where(e => e.EntryType == CashDrawerEntryType.CashSurplus).ToListAsync());
    }

    [SqlServerFact]
    public async Task MissingHistoricalRefundDoesNotChangeTheBalanceOfAPhysicallyClosedDrawer()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate(PreviousMigration);
        await using var db = database.Context();
        db.Users.Add(new ApplicationUser { Id = "u", UserName = "legacy" });
        db.Cashboxes.AddRange(new Cashbox { Name = "Drawer", IsDrawer = true, InitialBalance = 100 },
            new Cashbox { Name = "Treasury" });
        var cancelled = Order(1, "u", null, cancelled: true);
        cancelled.CancelledAt = BusinessClock.Now.AddDays(-1);
        db.Orders.Add(cancelled);
        await db.SaveChangesAsync();
        db.CashDrawerEntries.AddRange(Entry(1, 1, 20, CashDrawerEntryType.SalePayment),
            Entry(1, 1, 10, CashDrawerEntryType.Refund), Entry(null, 1, 110, CashDrawerEntryType.TransferOut),
            Entry(null, 2, 110, CashDrawerEntryType.TransferIn));
        await db.SaveChangesAsync();
        db.CashShifts.Add(new CashShift { SourceCashboxId = 1, TargetCashboxId = 2, UserId = "u", ClientRequestId = "closed",
            LastEntryId = await db.CashDrawerEntries.Where(e => e.CashboxId == 1).MaxAsync(e => e.Id),
            StartedAt = BusinessClock.Now.AddDays(-2), ClosedAt = BusinessClock.Now, ExpectedCash = 110, ActualCash = 110 });
        await db.SaveChangesAsync();
        await database.Migrate();
        db.ChangeTracker.Clear();
        var entries = await db.CashDrawerEntries.AsNoTracking().ToListAsync();
        Assert.Equal(20, entries.Where(e => e.EntryType == CashDrawerEntryType.Refund).Sum(e => e.Amount));
        Assert.Equal(0, CashLedger.Balance(100, entries.Where(e => e.CashboxId == 1)));
        Assert.Equal(110, CashLedger.Balance(0, entries.Where(e => e.CashboxId == 2)));
        Assert.Equal(10, Assert.Single(entries, e => e.EntryType == CashDrawerEntryType.CashSurplus).Amount);
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [__LegacyLedgerRepairs] WHERE [CashAdjustmentEntryId] IS NOT NULL").SingleAsync());
    }

    [SqlServerFact]
    public async Task DowngradePreservesPerUserRequestKeysAndUpgradeRestoresThem()
    {
        await using var database = await SqlDatabase.Create();
        await database.Migrate();
        await using var db = database.Context();
        db.Users.AddRange(new ApplicationUser { Id = "u", UserName = "first" }, new ApplicationUser { Id = "v", UserName = "second" });
        var first = Order(1, "u", "first"); first.ClientRequestId = new string('x', 64);
        var second = Order(2, "v", "second"); second.ClientRequestId = first.ClientRequestId;
        db.Orders.AddRange(first, second);
        await db.SaveChangesAsync();
        await database.Migrate("20261009121534_FixCashLedgerAndShiftClosures");
        var downgradedKeys = await db.Database.SqlQueryRaw<string>("SELECT [ClientRequestId] AS [Value] FROM [Orders]").ToListAsync();
        Assert.Equal(2, downgradedKeys.Distinct().Count());
        Assert.Equal(first.ClientRequestId, await db.Database.SqlQueryRaw<string>("SELECT [OriginalKey] AS [Value] FROM [__OrderRequestKeyBackups]").SingleAsync());
        await database.Migrate();
        Assert.All(await db.Orders.AsNoTracking().ToListAsync(), o => Assert.Equal(first.ClientRequestId, o.ClientRequestId));
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static Order Order(int id, string user, string? fingerprint, bool cancelled = false) => new()
    {
        OrderNumber = id + 100, UserId = user, RequestFingerprint = fingerprint,
        Total = 10, Profit = 10, Date = BusinessClock.Now, Notes = "",
        OrderStatus = cancelled ? OrderStatus.Cancelled : OrderStatus.Preparing
    };

    private static CashDrawerEntry Entry(int? order, int cashbox, decimal amount, CashDrawerEntryType type, bool automatic = false) => new()
    {
        OrderId = order, CashboxId = cashbox, Amount = amount, EntryType = type, UserId = "u", Date = BusinessClock.Now,
        AffectsCash = true, Notes = automatic ? "استرداد تلقائي لطلب ملغى سابقاً" : null
    };

    private sealed class CommitFault(bool afterCommit) : DbTransactionInterceptor
    {
        private bool failed;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!afterCommit && !failed) { failed = true; throw new TimeoutException("Lost connection before commit"); }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (afterCommit && !failed) { failed = true; throw new TimeoutException("Lost commit acknowledgement"); }
            return Task.CompletedTask;
        }
    }

    private sealed class RollbackFault : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionRollingBackAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Rollback connection lost");
    }

    private sealed class ReceiptCleanupFault : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("__TransactionReceipts", StringComparison.Ordinal))
                throw new InvalidOperationException("Injected receipt cleanup failure");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SqlDatabase : IAsyncDisposable
    {
        private readonly string name = "RestaurantDalTests_" + Guid.NewGuid().ToString("N");
        private string Connection(string database) => new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("RESTAURANT_TEST_SQLSERVER")) { InitialCatalog = database, ConnectTimeout = 5 }.ConnectionString;
        internal string ConnectionString => Connection(name);
        internal RestaurantDbContext Context(IInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(ConnectionString,
                o => o.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), [1205]));
            if (interceptor != null) options.AddInterceptors(interceptor);
            return new(options.Options);
        }
        internal static async Task<SqlDatabase> Create()
        {
            var database = new SqlDatabase();
            await using var connection = new SqlConnection(database.Connection("master"));
            await connection.OpenAsync();
            await using var command = new SqlCommand($"CREATE DATABASE [{database.name}]", connection);
            await command.ExecuteNonQueryAsync();
            return database;
        }
        internal async Task Migrate(string? target = null)
        {
            await using var db = Context();
            await db.GetService<IMigrator>().MigrateAsync(target);
        }
        public async ValueTask DisposeAsync()
        {
            if (!Regex.IsMatch(name, "^RestaurantDalTests_[a-f0-9]{32}$")) throw new InvalidOperationException("Unsafe test cleanup target");
            SqlConnection.ClearAllPools();
            await using var connection = new SqlConnection(Connection("master"));
            await connection.OpenAsync();
            await using var command = new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESTAURANT_TEST_SQLSERVER")))
            Skip = "Set RESTAURANT_TEST_SQLSERVER to run SQL Server regression tests.";
    }
}
