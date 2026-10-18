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
using Restaurant.Models.DTOs;
using Restaurant.Models;
using Restaurant.Services.IServices;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;

namespace Restaurant.Services.Tests;

public class TreasuryApiTests
{
    [Fact]
    public async Task ManualRequestRecovery_IsRestrictedToItsAuthenticatedOwner()
    {
        await using var test = await ApiFixture.Create("Permission.Treasury");
        var request = TreasuryTests.Entry(10);
        var created = await test.Client.PostAsJsonAsync("/api/cashdrawerentries", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var recovered = await test.Client.GetFromJsonAsync<CashDrawerEntryDto>($"/api/cashdrawerentries/byrequest/{request.ClientRequestId}");
        Assert.Equal(10m, recovered!.Amount);
        test.Client.DefaultRequestHeaders.Remove("X-Test-User"); test.Client.DefaultRequestHeaders.Add("X-Test-User", "other");
        Assert.Equal(HttpStatusCode.NotFound, (await test.Client.GetAsync($"/api/cashdrawerentries/byrequest/{request.ClientRequestId}")).StatusCode);
    }

    [Fact]
    public async Task RestaurantIdentity_UsesStoredAssociationAndLatestConfiguredName()
    {
        await using var test = await ApiFixture.Create("Permission.Treasury");
        test.Database.Db.Restaurants.AddRange(new RestaurantInfo { Id = 1, Name = "مطعم آخر" },
            new RestaurantInfo { Id = 2, Name = "مطعم النخيل & العائلة" });
        var user = await test.Database.Db.Users.SingleAsync();
        user.RestaurantId = 2;
        await test.Database.Db.SaveChangesAsync();
        var restaurant = await test.Client.GetFromJsonAsync<RestaurantDto>("/api/restaurants/current");
        Assert.Equal(2, restaurant!.Id);
        Assert.Equal("مطعم النخيل & العائلة", restaurant.Name);
        (await test.Database.Db.Restaurants.SingleAsync(r => r.Id == 2)).Name = "الاسم الجديد";
        await test.Database.Db.SaveChangesAsync();
        Assert.Equal("الاسم الجديد", (await test.Client.GetFromJsonAsync<RestaurantDto>("/api/restaurants/current"))!.Name);
    }

    [Fact]
    public async Task RestaurantIdentity_LegacySingleRestaurantAccountGetsConfiguredRestaurant()
    {
        await using var test = await ApiFixture.Create("Permission.NewOrder");
        test.Database.Db.Restaurants.Add(new RestaurantInfo { Id = 1, Name = "مطعم السرايا" });
        await test.Database.Db.SaveChangesAsync();
        Assert.Equal("مطعم السرايا", (await test.Client.GetFromJsonAsync<RestaurantDto>("/api/restaurants/current"))!.Name);
    }

    [Fact]
    public async Task RestaurantIdentity_DoesNotGuessWithMultipleRestaurants_AndRequiresLogin()
    {
        await using var test = await ApiFixture.Create("Permission.NewOrder");
        test.Database.Db.Restaurants.AddRange(new RestaurantInfo { Id = 1, Name = "الأول" }, new RestaurantInfo { Id = 2, Name = "الثاني" });
        await test.Database.Db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await test.Client.GetAsync("/api/restaurants/current")).StatusCode);
        test.Client.DefaultRequestHeaders.Remove("X-Test-User");
        Assert.Equal(HttpStatusCode.Unauthorized, (await test.Client.GetAsync("/api/restaurants/current")).StatusCode);
    }
    [Fact]
    public async Task WebManualDepositWithoutUserId_Succeeds_AndOverdrawReturnsConflict()
    {
        await using var test = await ApiFixture.Create("Permission.Treasury");
        var response = await test.Client.PostAsJsonAsync("/api/cashdrawerentries", TreasuryTests.Entry(10));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var entry = await response.Content.ReadFromJsonAsync<CashDrawerEntryDto>();
        Assert.Equal("u", entry!.UserId);
        var overdraw = await test.Client.PostAsJsonAsync("/api/cashdrawerentries", TreasuryTests.Entry(500, Restaurant.Models.Enums.CashDrawerEntryType.Outflow));
        Assert.Equal(HttpStatusCode.Conflict, overdraw.StatusCode);
    }

    [Fact]
    public async Task Cashier_CanBlindCloseWithZero_ButCannotReadOrWriteTreasury()
    {
        await using var test = await ApiFixture.Create("Permission.NewOrder");
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.GetAsync("/api/cashdrawerentries")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.GetAsync("/api/cashboxes/1/balance")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.PostAsJsonAsync("/api/cashdrawerentries", TreasuryTests.Entry(10))).StatusCode);
        var boxes = await test.Client.GetFromJsonAsync<List<CashboxDto>>("/api/cashboxes");
        Assert.All(boxes!, b => Assert.Equal(0m, b.InitialBalance));
        var ctx = await test.Client.GetFromJsonAsync<CashShiftContextDto>("/api/cashshifts/context/1");
        Assert.Null(ctx!.ExpectedCash);
        var request = new CashShiftCloseDto { SourceCashboxId = 1, TargetCashboxId = 2, ActualCash = 0,
            PreviousShiftId = ctx.PreviousShiftId, LastEntryId = ctx.LastEntryId, ClientRequestId = Guid.NewGuid().ToString("N") };
        var response = await test.Client.PostAsJsonAsync("/api/cashshifts/close", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<CashShiftDto>();
        Assert.Null(saved!.ExpectedCash);
        Assert.Null(saved.Difference);
        Assert.Equal(0m, saved.ActualCash);
        var retry = await test.Client.PostAsJsonAsync("/api/cashshifts/close", request);
        Assert.Equal(saved.Id, (await retry.Content.ReadFromJsonAsync<CashShiftDto>())!.Id);
        var recovered = await test.Client.GetFromJsonAsync<CashShiftDto>($"/api/cashshifts/byrequest/{request.ClientRequestId}");
        Assert.Equal(saved.Id, recovered!.Id);
        Assert.Null(recovered.ExpectedCash);
        test.Client.DefaultRequestHeaders.Remove("X-Test-User");
        test.Client.DefaultRequestHeaders.Add("X-Test-User", "other-user");
        Assert.Equal(HttpStatusCode.NotFound, (await test.Client.GetAsync($"/api/cashshifts/byrequest/{request.ClientRequestId}")).StatusCode);
    }

    [Fact]
    public async Task UnprivilegedUser_CannotCloseShift_AndInvalidTargetDoesNotWrite()
    {
        await using var test = await ApiFixture.Create("");
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.GetAsync("/api/cashshifts/context/1")).StatusCode);
        test.Client.DefaultRequestHeaders.Add("X-Test-Permissions", "Permission.NewOrder");
        var request = new CashShiftCloseDto { SourceCashboxId = 1, TargetCashboxId = 1, ActualCash = 100,
            ClientRequestId = Guid.NewGuid().ToString("N") };
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PostAsJsonAsync("/api/cashshifts/close", request)).StatusCode);
        Assert.Equal(0, await test.Database.Db.CashShifts.CountAsync());
        Assert.Equal(0, await test.Database.Db.CashDrawerEntries.CountAsync());
    }

    private sealed class ApiFixture : IAsyncDisposable
    {
        internal TreasuryDatabase Database { get; }
        private readonly WebApplication _app;
        internal HttpClient Client { get; }
        private ApiFixture(TreasuryDatabase database, WebApplication app) { Database = database; _app = app; Client = app.GetTestClient(); }
        internal static async Task<ApiFixture> Create(string permissions)
        {
            var database = await TreasuryDatabase.CreateAsync();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddControllers().AddApplicationPart(typeof(CashboxesController).Assembly);
            builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
            builder.Services.AddAuthorization(TreasuryAuthorization.Configure);
            builder.Services.AddDbContext<RestaurantDbContext>(o => o.UseSqlite(database.Connection));
            builder.Services.AddSingleton(database.Mapper);
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICashDrawerEntryService, CashDrawerEntryService>();
            builder.Services.AddScoped<ICashboxService, CashboxService>();
            builder.Services.AddScoped<ICashShiftService, CashShiftService>();
            builder.Services.AddScoped<IRestaurantService, RestaurantService>();
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            var fixture = new ApiFixture(database, app);
            fixture.Client.DefaultRequestHeaders.Add("X-Test-User", "u");
            if (permissions.Length > 0) fixture.Client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
            return fixture;
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await _app.DisposeAsync(); await Database.DisposeAsync(); }
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var user)) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.ToString()) };
            claims.AddRange(Request.Headers["X-Test-Permissions"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => new Claim("Permission", p)));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), "Test")));
        }
    }
}
