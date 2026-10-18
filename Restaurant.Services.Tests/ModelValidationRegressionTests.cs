using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Restaurant.API;
using Restaurant.API.Controllers;
using Restaurant.DataAccess;
using Restaurant.DataAccess.Repositories;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;

namespace Restaurant.Services.Tests;

public class ModelValidationRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrNullActualCashCannotCloseShift(bool sendNull)
    {
        await using var test = await ApiFixture.Create();
        var payload = new Dictionary<string, object?>
        {
            ["sourceCashboxId"] = 1, ["targetCashboxId"] = 2,
            ["previousShiftId"] = 0, ["lastEntryId"] = 0, ["clientRequestId"] = "missing-count"
        };
        if (sendNull) payload["actualCash"] = null;

        var response = await test.Client.PostAsJsonAsync("/api/cashshifts/close", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await test.Database.Db.CashShifts.ToListAsync());
        Assert.Empty(await test.Database.Db.CashDrawerEntries.ToListAsync());
        Assert.Equal(100m, await test.Database.Balance(1));
    }

    [Fact]
    public async Task ShiftServiceAlsoRejectsMissingActualCash()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var dto = await test.CloseRequest(0);
        dto.ActualCash = null;

        await Assert.ThrowsAsync<ArgumentException>(() => test.Shifts.CloseAsync(dto, "u", true));

        Assert.Empty(await test.Db.CashShifts.ToListAsync());
        Assert.Empty(await test.Db.CashDrawerEntries.ToListAsync());
        Assert.Equal(100m, await test.Balance(1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NullOrderItemsReturnBadRequestWithoutWriting(bool checkout)
    {
        await using var test = await ApiFixture.Create();
        var order = new { orderItems = new object?[] { null } };
        object payload = checkout
            ? new { cashboxId = 1, paymentMethodId = 1, clientRequestId = "null-item", order }
            : order;

        var response = await test.Client.PostAsJsonAsync(checkout ? "/api/orders/checkout" : "/api/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await test.Database.Db.Orders.ToListAsync());
        Assert.Empty(await test.Database.Db.CashDrawerEntries.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrderServicesRejectNullItemsBeforeFingerprintingOrWriting(bool checkout)
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = new OrderCreateDto { OrderItems = [null!] };

        if (checkout)
            await Assert.ThrowsAsync<ArgumentException>(() => test.Orders.CheckoutAsync(new()
                { CashboxId = 1, PaymentMethodId = 1, ClientRequestId = "null-item", Order = order }, "u", true));
        else
            await Assert.ThrowsAsync<ArgumentException>(() => test.Orders.CreateOrderAsync(order, "u", true));

        Assert.Empty(await test.Db.Orders.ToListAsync());
        Assert.Empty(await test.Db.CashDrawerEntries.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServerGeneratedOrderFieldsMayBeOmitted(bool checkout)
    {
        await using var test = await ApiFixture.Create();
        var order = new { orderItems = new[] { new { productId = 1, quantity = 1 } } };
        object payload = checkout
            ? new { cashboxId = 1, paymentMethodId = 1, clientRequestId = "server-fields", order }
            : order;

        var response = await test.Client.PostAsJsonAsync(checkout ? "/api/orders/checkout" : "/api/orders", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal("u", saved.UserId);
        Assert.True(saved.OrderNumber > 0);
        Assert.Equal(10m, saved.Total);
        Assert.Equal(checkout ? 1 : 0, await test.Database.Db.CashDrawerEntries.CountAsync());
    }

    [Fact]
    public async Task LegacyOrderFieldsCannotOverrideServerValues()
    {
        await using var test = await ApiFixture.Create();
        var request = TreasuryTests.Checkout(1);
        request.Order.OrderNumber = -1;
        request.Order.UserId = "forged-user";

        var response = await test.Client.PostAsJsonAsync("/api/orders/checkout", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal("u", saved.UserId);
        Assert.True(saved.OrderNumber > 0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task InvalidPrinterEnumsAreRejectedOnCreateAndUpdate(int type)
    {
        await using var test = await ApiFixture.Create();
        var original = await test.Printers.CreatePrinterAsync(PrinterRequest("Original"));
        var create = PrinterRequest("Invalid");
        create.PrinterType = (PrinterType)type;

        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PostAsJsonAsync("/api/printers", create)).StatusCode);
        var update = new PrinterUpdateDto { Id = original.Id, Name = "Changed", PrinterName = "Original",
            PrintStationId = 1, PrinterType = (PrinterType)type };
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PutAsJsonAsync($"/api/printers/{original.Id}", update)).StatusCode);
        await Assert.ThrowsAsync<ArgumentException>(() => test.Printers.CreatePrinterAsync(create));
        await Assert.ThrowsAsync<ArgumentException>(() => test.Printers.UpdatePrinterAsync(update));

        var saved = Assert.Single(await test.Database.Db.Printers.AsNoTracking().ToListAsync());
        Assert.Equal(PrinterType.Kitchen, saved.PrinterType);
        Assert.Equal("Original", saved.Name);
    }

    [Fact]
    public async Task DuplicatePrinterCreationAndUpdatesAreRejectedWithoutChangingConfiguration()
    {
        await using var test = await ApiFixture.Create();
        var first = await test.Printers.CreatePrinterAsync(PrinterRequest(" TestQueue "));
        Assert.Equal("TestQueue", first.PrinterName);
        Assert.Equal(HttpStatusCode.Conflict,
            (await test.Client.PostAsJsonAsync("/api/printers", PrinterRequest("testqueue"))).StatusCode);
        var second = await test.Printers.CreatePrinterAsync(PrinterRequest("OtherQueue"));
        var update = new PrinterUpdateDto { Id = second.Id, Name = "Duplicate", PrinterName = " TestQueue ",
            PrintStationId = 1, PrinterType = PrinterType.Kitchen };

        Assert.Equal(HttpStatusCode.Conflict,
            (await test.Client.PutAsJsonAsync($"/api/printers/{second.Id}", update)).StatusCode);
        update.Id = first.Id;
        Assert.Equal(HttpStatusCode.NoContent,
            (await test.Client.PutAsJsonAsync($"/api/printers/{first.Id}", update)).StatusCode);

        var saved = await test.Database.Db.Printers.AsNoTracking().ToListAsync();
        Assert.Equal(2, saved.Count);
        Assert.Equal("OtherQueue", saved.Single(p => p.Id == second.Id).PrinterName);
    }

    [Fact]
    public async Task PrinterCanBeReusedForDifferentTypesAndStations()
    {
        await using var test = await ApiFixture.Create();
        await test.Printers.CreatePrinterAsync(PrinterRequest("Queue"));
        var receipt = PrinterRequest("Queue");
        receipt.PrinterType = PrinterType.Receipt;
        await test.Printers.CreatePrinterAsync(receipt);
        test.Database.Db.PrintStations.Add(new PrintStation { Id = 2, Name = "Second station" });
        await test.Database.Db.SaveChangesAsync();
        var otherStation = PrinterRequest("Queue");
        otherStation.PrintStationId = 2;
        await test.Printers.CreatePrinterAsync(otherStation);

        Assert.Equal(3, await test.Database.Db.Printers.CountAsync());
    }

    [Fact]
    public async Task DatabaseUniqueIndexBlocksDuplicatesWhenServiceIsBypassed()
    {
        await using var test = await ApiFixture.Create();
        await test.Printers.CreatePrinterAsync(PrinterRequest("Queue"));
        test.Database.Db.Printers.Add(new Printer { Name = "Another label", PrinterName = "Queue",
            PrintStationId = 1, PrinterType = PrinterType.Kitchen });

        await Assert.ThrowsAsync<DbUpdateException>(() => test.Database.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task UnknownPrinterStationReturnsBadRequest()
    {
        await using var test = await ApiFixture.Create();
        var request = PrinterRequest("Queue");
        request.PrintStationId = 999;

        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PostAsJsonAsync("/api/printers", request)).StatusCode);
        Assert.Empty(await test.Database.Db.Printers.ToListAsync());
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(2025)]
    public void ReportPeriodCountsBothBoundaryDays(int year)
    {
        var start = new DateTime(year, 1, 1);
        var allowed = ReportPeriod.Create(start, start.AddDays(365));
        Assert.Equal(366, (allowed.EndExclusive - allowed.Start).TotalDays);
        Assert.Throws<ArgumentException>(() => ReportPeriod.Create(start, start.AddDays(366)));
        var singleDay = ReportPeriod.Create(start, start);
        Assert.Equal(1, (singleDay.EndExclusive - singleDay.Start).TotalDays);
    }

    [Fact]
    public async Task OrderEndDateFilterDoesNotRequireAStartDateOrExcludeOlderOrders()
    {
        await using var test = await TreasuryDatabase.CreateAsync();
        var order = await test.Orders.CreateOrderAsync(new OrderCreateDto
            { OrderItems = [new OrderItemCreateDto { ProductId = 1, Quantity = 1 }] }, "u", true);
        (await test.Db.Orders.SingleAsync(o => o.Id == order.Id)).Date = BusinessClock.Today.AddYears(-2);
        await test.Db.SaveChangesAsync();

        var page = await test.Orders.GetOrdersPageAsync(new OrderQueryDto { To = BusinessClock.Today });
        Assert.Equal(order.Id, Assert.Single(page.Items).Id);
        var empty = await test.Orders.GetOrdersPageAsync(new OrderQueryDto { To = DateTime.MinValue.Date });
        Assert.Empty(empty.Items);
    }

    [Fact]
    public void SqlServerModelMatchesMigrationSnapshot()
    {
        using var db = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer("Server=unused;Database=ModelCheck;Integrated Security=true;TrustServerCertificate=true").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static PrinterCreateDto PrinterRequest(string queue) => new()
        { Name = queue, PrinterName = queue, PrinterType = PrinterType.Kitchen, PrintStationId = 1 };

    private sealed class ApiFixture : IAsyncDisposable
    {
        internal TreasuryDatabase Database { get; }
        internal HttpClient Client { get; }
        internal PrinterService Printers => new(Database.Unit, Database.Mapper);
        private readonly WebApplication app;

        private ApiFixture(TreasuryDatabase database, WebApplication application)
            { Database = database; app = application; Client = app.GetTestClient(); }

        internal static async Task<ApiFixture> Create()
        {
            var database = await TreasuryDatabase.CreateAsync();
            database.Db.PrintStations.Add(new PrintStation { Id = 1, Name = "Kitchen" });
            await database.Db.SaveChangesAsync();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);
            builder.Services.AddDbContext<RestaurantDbContext>(o => o.UseSqlite(database.Connection));
            builder.Services.AddSingleton(database.Mapper);
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPrinterService, PrinterService>();
            builder.Services.AddScoped<ICashShiftService, CashShiftService>();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, AdminAuthentication>("Test", _ => { });
            builder.Services.AddAuthorization(PermissionAuthorization.Configure);
            var app = builder.Build();
            app.UseExceptionHandler(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
            await app.StartAsync();
            return new(database, app);
        }

        public async ValueTask DisposeAsync()
            { Client.Dispose(); await app.DisposeAsync(); await Database.DisposeAsync(); }
    }

    private sealed class AdminAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "u"), new Claim(ClaimTypes.Role, AppRoles.Admin) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), "Test")));
        }
    }
}
