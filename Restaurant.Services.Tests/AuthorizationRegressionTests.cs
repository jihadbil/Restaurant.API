using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
using System.Text;
using SkiaSharp;
using Xunit;

namespace Restaurant.Services.Tests;

public class AuthorizationRegressionTests
{
    [Fact]
    public async Task DuplicateEmailUpdateIsRejectedAndBothUsersCanStillLogIn()
    {
        await using var test = await Fixture.Create();
        var first = await test.Register("email-first", []);
        var second = await test.Register("email-second", []);
        test.UseToken((await test.Register("email-admin", [], [AppRoles.Admin])).Token);
        var response = await test.Client.PutAsJsonAsync($"/api/users/{second.User!.Id}", new UserUpdateDto
        {
            UserName = second.User.UserName!, Email = first.User!.Email!.ToUpperInvariant(), Roles = [AppRoles.Admin]
        });
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        Assert.Contains("البريد الإلكتروني", await response.Content.ReadAsStringAsync());
        foreach (var account in new[] { first, second })
        {
            var login = await test.Auth.LoginAsync(new() { UserName = account.User!.Email!, Password = "Audit@123456" });
            Assert.True(login.IsSuccess, login.Message);
            Assert.Equal(account.User.Id, login.User!.Id);
        }
        Assert.DoesNotContain(AppRoles.Admin, (await test.Users.GetUserByIdAsync(second.User.Id))!.Roles);
    }

    [Fact]
    public async Task CategoryRoutingReplacesAndClearsEveryStationWithDelegatedPermission()
    {
        await using var test = await Fixture.Create();
        test.Database.Db.PrintStations.AddRange(new PrintStation { Id = 10, Name = "Kitchen A" }, new PrintStation { Id = 11, Name = "Kitchen B" });
        await test.Database.Db.SaveChangesAsync();
        test.UseToken((await test.Register("routing-manager", [AppPermissions.ManageCategories])).Token);
        var path = "/api/categoryprintstations/category/1";
        Assert.Equal(HttpStatusCode.NoContent, (await test.Client.PutAsJsonAsync(path, new { printStationIds = new[] { 10, 11 } })).StatusCode);
        Assert.Equal(new[] { 10, 11 }, (await test.Client.GetFromJsonAsync<List<PrintStationDto>>(path))!.Select(s => s.Id).Order());
        Assert.Equal(HttpStatusCode.NoContent, (await test.Client.PutAsJsonAsync(path, new { printStationIds = new[] { 10, 11 } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await test.Client.PutAsJsonAsync(path, new { printStationIds = Array.Empty<int>() })).StatusCode);
        Assert.Empty((await test.Client.GetFromJsonAsync<List<PrintStationDto>>(path))!);
        test.UseToken((await test.Register("routing-reader", [AppPermissions.Categories])).Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.PutAsJsonAsync(path, new { printStationIds = new[] { 10 } })).StatusCode);
    }

    [Fact]
    public async Task FailedRoutingReplacementKeepsOriginalStations()
    {
        await using var test = await Fixture.Create();
        test.Database.Db.PrintStations.AddRange(new PrintStation { Id = 10, Name = "Old" }, new PrintStation { Id = 11, Name = "New" });
        test.Database.Db.CategoryPrintStations.Add(new CategoryPrintStation { CategoryId = 1, PrintStationId = 10 });
        await test.Database.Db.SaveChangesAsync();
        test.UseToken((await test.Register("routing-admin", [], [AppRoles.Admin])).Token);
        var path = "/api/categoryprintstations/category/1";
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PutAsJsonAsync(path, new { printStationIds = new[] { 999 } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PutAsJsonAsync(path, new { printStationIds = (int[]?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PutAsJsonAsync(path, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PutAsJsonAsync(path, new { printStationIds = new[] { -1 } })).StatusCode);
        await test.Database.Db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER RejectRouting BEFORE INSERT ON CategoryPrintStations WHEN NEW.PrintStationId = 11
            BEGIN SELECT RAISE(ABORT, 'Simulated routing write failure'); END;
            """);
        Assert.Equal(HttpStatusCode.Conflict, (await test.Client.PutAsJsonAsync(path, new { printStationIds = new[] { 11 } })).StatusCode);
        Assert.Equal(10, Assert.Single((await test.Client.GetFromJsonAsync<List<PrintStationDto>>(path))!).Id);
    }

    [Theory]
    [InlineData("products/upload-image")]
    [InlineData("restaurants/upload-logo")]
    [InlineData("paymentmethods/upload-logo")]
    public async Task UploadRejectsHeaderOnlyAndTruncatedImagesWithoutSavingFiles(string route)
    {
        await using var test = await Fixture.Create();
        test.UseToken((await test.Register("image-admin", [], [AppRoles.Admin])).Token);
        var png = ImageBytes(SKEncodedImageFormat.Png);
        foreach (var bytes in new[] { new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, new byte[] { 255, 216, 255 }, png[..(png.Length / 2)] })
        {
            using var form = ImageForm(bytes);
            Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PostAsync("/api/" + route, form)).StatusCode);
        }
        Assert.Empty(Directory.GetFiles(test.WebRoot, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png, ".png")]
    [InlineData(SKEncodedImageFormat.Jpeg, ".jpg")]
    public async Task UploadFullyDecodesValidPngAndJpeg(SKEncodedImageFormat format, string extension)
    {
        await using var test = await Fixture.Create();
        test.UseToken((await test.Register("valid-image-admin", [], [AppRoles.Admin])).Token);
        var bytes = ImageBytes(format);
        using var form = ImageForm(bytes);
        var response = await test.Client.PostAsync("/api/products/upload-image", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = Assert.Single(Directory.GetFiles(test.WebRoot, "*", SearchOption.AllDirectories));
        Assert.EndsWith(extension, saved);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(saved));
    }

    [Fact]
    public async Task UploadRejectsExcessiveDimensionsBeforeSaving()
    {
        await using var test = await Fixture.Create();
        test.UseToken((await test.Register("large-image-admin", [], [AppRoles.Admin])).Token);
        using var form = ImageForm(ImageBytes(SKEncodedImageFormat.Png, 8193, 1));
        Assert.Equal(HttpStatusCode.BadRequest, (await test.Client.PostAsync("/api/products/upload-image", form)).StatusCode);
        Assert.Empty(Directory.GetFiles(test.WebRoot, "*", SearchOption.AllDirectories));
    }

    private static byte[] ImageBytes(SKEncodedImageFormat format, int width = 2, int height = 2)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, 90);
        return encoded.ToArray();
    }

    private static MultipartFormDataContent ImageForm(byte[] bytes)
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", "upload.data");
        return form;
    }

    [Fact]
    public async Task CatalogManagementIsDelegated_WhileCostDataIsHiddenFromCashiers()
    {
        await using var test = await Fixture.Create();
        test.UseToken((await test.Register("reader", [AppPermissions.NewOrder])).Token);
        var product = (await test.Client.GetFromJsonAsync<ProductDto>("/api/products/1"))!;
        Assert.False(product.CanViewCosts); Assert.Equal(0m, product.CostPrice); Assert.Equal(0m, product.Profit);
        var dto = new ProductCreateDto { Name = "Managed meal", SalePrice = 5, CostPrice = 2, CategoryId = 1 };
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.PostAsJsonAsync("/api/products", dto)).StatusCode);
        test.UseToken((await test.Register("catalog-manager", [AppPermissions.ManageProducts])).Token);
        Assert.Equal(HttpStatusCode.Created, (await test.Client.PostAsJsonAsync("/api/products", dto)).StatusCode);
        Assert.True((await test.Client.GetFromJsonAsync<ProductDto>("/api/products/1"))!.CanViewCosts);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutPermissionsCannotReadWriteOrCheckoutOrders()
    {
        await using var test = await Fixture.Create();
        var account = await test.Register("restricted", []);
        test.UseToken(account.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.GetAsync("/api/orders")).StatusCode);
        var request = TreasuryTests.Checkout(1); request.Order.OrderNumber = 12345;
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.PostAsJsonAsync("/api/orders/checkout", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.PutAsJsonAsync("/api/orders/1", new OrderUpdateDto { Id = 1 })).StatusCode);
        Assert.Equal(0, await test.Database.Db.Orders.CountAsync());
    }

    [Fact]
    public async Task RegistrationNormalizesLegacyPermissionsAndPersistsSelectedRole()
    {
        await using var test = await Fixture.Create();
        var account = await test.Register("manager", ["POS", "Reports"], [AppRoles.Admin]);
        Assert.Contains(AppRoles.Admin, account.User!.Roles);
        Assert.Contains(AppPermissions.NewOrder, account.User.Permissions);
        Assert.Contains(AppPermissions.Reports, account.User.Permissions);
        Assert.DoesNotContain("POS", account.User.Permissions);
        test.UseToken(account.Token);
        Assert.Equal(HttpStatusCode.OK, (await test.Client.GetAsync("/api/reports/comprehensive")).StatusCode);
        var rejected = await test.Auth.RegisterAsync(new RegisterRequestDto { UserName = "invalid", Email = "invalid@example.invalid", Password = "Audit@123456", Permissions = ["NotARealPermission"] }, null);
        Assert.False(rejected.IsSuccess);
        Assert.False(await test.Database.Db.Users.AnyAsync(u => u.UserName == "invalid"));
    }

    [Fact]
    public async Task RoleAndPermissionChangesInvalidatePreviouslyIssuedTokens()
    {
        await using var test = await Fixture.Create();
        await test.Register("backup", [], [AppRoles.Admin]);
        var account = await test.Register("manager", [AppPermissions.NewOrder], [AppRoles.Admin]);
        test.UseToken(account.Token);
        Assert.True(await test.Users.UpdateUserRolesAsync(account.User!.Id, [AppRoles.Cashier]));
        Assert.Equal(HttpStatusCode.Unauthorized, (await test.Client.GetAsync("/api/products")).StatusCode);
        var login = await test.Auth.LoginAsync(new() { UserName = "manager", Password = "Audit@123456" });
        test.UseToken(login.Token);
        Assert.True(await test.Users.UpdateUserAsync(account.User.Id, new UserUpdateDto { UserName = "manager", Email = "manager@example.invalid", Permissions = [] }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await test.Client.GetAsync("/api/products")).StatusCode);
    }

    [Fact]
    public async Task ResetPasswordInvalidatesOldToken()
    {
        await using var test = await Fixture.Create();
        var account = await test.Register("cashier", [AppPermissions.NewOrder]);
        test.UseToken(account.Token);
        Assert.True(await test.Users.ResetPasswordAsync(account.User!.Id, "Changed@123456"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await test.Client.GetAsync("/api/products")).StatusCode);
    }

    [Fact]
    public async Task DashboardPermissionWorksWithoutGrantingReportsAccess()
    {
        await using var test = await Fixture.Create();
        test.UseToken((await test.Register("dashboard", [AppPermissions.Dashboard])).Token);
        Assert.Equal(HttpStatusCode.OK, (await test.Client.GetAsync("/api/reports/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.GetAsync("/api/reports/comprehensive")).StatusCode);
    }

    [Fact]
    public async Task CashierCanCancelPaidOrderByPermissionWithoutAdminRole()
    {
        await using var test = await Fixture.Create();
        var account = await test.Register("operator", [AppPermissions.NewOrder, AppPermissions.Orders, AppPermissions.VoidOrder]);
        test.UseToken(account.Token);
        var payload = TreasuryTests.Checkout(1); payload.Order.OrderNumber = 12345;
        var response = await test.Client.PostAsJsonAsync("/api/orders/checkout", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal(0m, order.Cost);
        Assert.Equal(0m, Assert.Single(order.OrderItems).UnitCostPrice);
        Assert.Equal(HttpStatusCode.NoContent, (await test.Client.PutAsJsonAsync($"/api/orders/{order.Id}",
            new OrderUpdateDto { Id = order.Id, OrderStatus = OrderStatus.Cancelled, Discount = order.Discount })).StatusCode);
        Assert.Equal(1, await test.Database.Db.CashDrawerEntries.CountAsync(e => e.EntryType == CashDrawerEntryType.Refund));
        Assert.Equal(HttpStatusCode.Forbidden, (await test.Client.DeleteAsync($"/api/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await test.Client.PostAsJsonAsync("/api/orders/checkout", new OrderCheckoutCreateDto {
            ClientRequestId = payload.ClientRequestId, CashboxId = 1, PaymentMethodId = 2, Order = payload.Order })).StatusCode);
    }

    [Fact]
    public async Task MalformedEnumAndForeignKeyReturnClientErrors()
    {
        await using var test = await Fixture.Create();
        test.UseToken((await test.Register("admin", [], [AppRoles.Admin])).Token);
        var invalid = await test.Client.PostAsJsonAsync("/api/products", new ProductCreateDto { Name = "Meal", CategoryId = 999, SalePrice = 1, CostPrice = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var dates = await test.Client.GetAsync("/api/reports/daily-sales?startDate=9999-12-31&endDate=9999-12-31");
        Assert.Equal(HttpStatusCode.BadRequest, dates.StatusCode);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal TreasuryDatabase Database { get; }
        private readonly WebApplication app;
        private readonly AsyncServiceScope scope;
        internal AuthService Auth { get; }
        internal UserService Users { get; }
        internal HttpClient Client { get; }
        internal string WebRoot { get; }
        private Fixture(TreasuryDatabase database, WebApplication application, AsyncServiceScope serviceScope, string webRoot)
        {
            Database = database; app = application; scope = serviceScope;
            Auth = scope.ServiceProvider.GetRequiredService<AuthService>();
            Users = scope.ServiceProvider.GetRequiredService<UserService>();
            Client = app.GetTestClient();
            WebRoot = webRoot;
        }
        internal static async Task<Fixture> Create()
        {
            var database = await TreasuryDatabase.CreateAsync();
            var webRoot = Directory.CreateTempSubdirectory("RestaurantImageTests_").FullName;
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = webRoot });
            builder.WebHost.UseTestServer(); builder.Logging.ClearProviders();
            const string key = "isolated-tests-signing-key-never-used-for-deployment-123456";
            builder.Configuration["Jwt:Key"] = key;
            builder.Configuration["Jwt:Issuer"] = "tests";
            builder.Configuration["Jwt:Audience"] = "tests";
            builder.Configuration["Jwt:DurationInMinutes"] = "60";
            builder.Services.AddDbContext<RestaurantDbContext>(o => o.UseSqlite(database.Connection));
            builder.Services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
                .AddRoles<IdentityRole>().AddEntityFrameworkStores<RestaurantDbContext>().AddDefaultTokenProviders();
            builder.Services.AddDataProtection();
            builder.Services.AddSingleton(database.Mapper);
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<AuthService>(); builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<IAuthService>(services => services.GetRequiredService<AuthService>());
            builder.Services.AddScoped<IUserService>(services => services.GetRequiredService<UserService>());
            builder.Services.AddScoped<IOrderService, OrderService>(); builder.Services.AddScoped<IOrderItemService, OrderItemService>();
            builder.Services.AddScoped<IProductService, ProductService>(); builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<ICategoryPrintStationService, CategoryPrintStationService>();
            builder.Services.AddScoped<IRestaurantService, RestaurantService>();
            builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
            builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            {
                o.Events = new JwtBearerEvents { OnTokenValidated = SessionTokenValidation.ValidateAsync };
                o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true,
                    ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = "tests", ValidAudience = "tests",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.Zero };
            });
            builder.Services.AddAuthorization(PermissionAuthorization.Configure);
            var app = builder.Build(); app.UseExceptionHandler(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
            await app.StartAsync();
            var scope = app.Services.CreateAsyncScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await roleManager.CreateAsync(new(AppRoles.Admin)); await roleManager.CreateAsync(new(AppRoles.Cashier));
            return new(database, app, scope, webRoot);
        }
        internal async Task<AuthResponseDto> Register(string name, string[] permissions, string[]? roles = null)
        {
            var result = await Auth.RegisterAsync(new RegisterRequestDto { UserName = name, Email = name + "@example.invalid",
                Password = "Audit@123456", Permissions = permissions, Roles = roles ?? [AppRoles.Cashier] }, null);
            Assert.True(result.IsSuccess, result.Message);
            return result;
        }
        internal void UseToken(string token) => Client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        public async ValueTask DisposeAsync()
        {
            Client.Dispose(); await scope.DisposeAsync(); await app.DisposeAsync(); await Database.DisposeAsync();
            Directory.Delete(WebRoot, recursive: true);
        }
    }
}
