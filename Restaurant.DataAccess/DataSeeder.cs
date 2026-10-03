using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.DataAccess;

public class DataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly RestaurantDbContext _db;
    private readonly IConfiguration _configuration;

    public DataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        RestaurantDbContext db,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Apply pending migrations if there are any
            if ((await _db.Database.GetPendingMigrationsAsync()).Any())
            {
                await _db.Database.MigrateAsync();
            }

            // Seed Roles
            if (!await _roleManager.RoleExistsAsync(AppRoles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
            }

            if (!await _roleManager.RoleExistsAsync(AppRoles.Cashier))
            {
                await _roleManager.CreateAsync(new IdentityRole(AppRoles.Cashier));
            }

            // Seed Default Restaurant
            var defaultRestaurant = await _db.Restaurants.FirstOrDefaultAsync();
            if (defaultRestaurant == null)
            {
                defaultRestaurant = new RestaurantInfo
                {
                    Name = "المطعم الافتراضي",
                    Address = "العنوان الافتراضي",
                    PhoneNumber = "0123456789",
                    TaxNumber = "123456789012345"
                };
                await _db.Restaurants.AddAsync(defaultRestaurant);
                await _db.SaveChangesAsync();
            }

            // إنشاء المدير الأول اختياري، وبياناته تأتي من user-secrets أو متغيرات البيئة فقط.
            var bootstrapUserName = _configuration["BootstrapAdmin:UserName"];
            var bootstrapEmail = _configuration["BootstrapAdmin:Email"];
            var bootstrapPassword = _configuration["BootstrapAdmin:Password"];
            if (!string.IsNullOrWhiteSpace(bootstrapUserName)
                && !string.IsNullOrWhiteSpace(bootstrapEmail)
                && !string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                var adminUser = await _userManager.FindByNameAsync(bootstrapUserName);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = bootstrapUserName,
                        Email = bootstrapEmail,
                        EmailConfirmed = true,
                        RestaurantId = defaultRestaurant.Id
                    };

                    var result = await _userManager.CreateAsync(adminUser, bootstrapPassword);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Bootstrap administrator could not be created: " +
                            string.Join(" | ", result.Errors.Select(error => error.Description)));
                    }

                    var roleResult = await _userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Bootstrap administrator role could not be assigned: " +
                            string.Join(" | ", roleResult.Errors.Select(error => error.Description)));
                    }
                    var allPermissions = new[] {
                        "Permission.Dashboard", "Permission.NewOrder", "Permission.Orders",
                        "Permission.Products", "Permission.Categories", "Permission.Reports",
                        "Permission.Settings", "Permission.Treasury",
                        "Permission.POS.ApplyDiscount", "Permission.POS.VoidItem",
                        "Permission.Orders.VoidOrder", "Permission.Shift.ViewTotals",
                        "Permission.Products.Manage", "Permission.Categories.Manage"
                    };
                    foreach (var permission in allPermissions)
                    {
                        await _userManager.AddClaimAsync(
                            adminUser,
                            new System.Security.Claims.Claim("Permission", permission));
                    }
                }
                else if (adminUser.RestaurantId == null)
                {
                    adminUser.RestaurantId = defaultRestaurant.Id;
                    await _userManager.UpdateAsync(adminUser);
                }

                if (!await _userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
                {
                    var roleResult = await _userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Bootstrap administrator role could not be assigned: " +
                            string.Join(" | ", roleResult.Errors.Select(error => error.Description)));
                    }
                }
            }

            await SecureLegacyDefaultAccountAsync(
                "admin",
                "Admin@123456",
                string.Equals(bootstrapUserName, "admin", StringComparison.OrdinalIgnoreCase)
                    ? bootstrapPassword
                    : null);
            await SecureLegacyDefaultAccountAsync("cashier", "Cashier@123456", replacementPassword: null);

            var administrators = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            var hasActiveAdministrator = false;
            foreach (var administrator in administrators)
            {
                if (!await _userManager.IsLockedOutAsync(administrator))
                {
                    hasActiveAdministrator = true;
                    break;
                }
            }

            if (!hasActiveAdministrator)
            {
                throw new InvalidOperationException(
                    "No active administrator account exists. Configure BootstrapAdmin:UserName, " +
                    "BootstrapAdmin:Email and BootstrapAdmin:Password through user-secrets or environment variables.");
            }

            // Seed Payment Methods
            if (!_db.PaymentMethods.Any())
            {
                await _db.PaymentMethods.AddRangeAsync(
                    new PaymentMethod { Name = "نقدي", IsTaxFree = false },
                    new PaymentMethod { Name = "بطاقة", IsTaxFree = false }
                );
                await _db.SaveChangesAsync();
            }

            // Seed Cashboxes
            if (!_db.Cashboxes.Any())
            {
                await _db.Cashboxes.AddAsync(
                    new Cashbox
                    {
                        Name = "الخزينة الرئيسية",
                        Description = "درج النقود الأساسي لنقطة البيع الرئيسية",
                        InitialBalance = 1000.00m,
                        IsActive = true
                    }
                );
                await _db.SaveChangesAsync();
            }

            // Seed Categories
            if (!_db.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "وجبات رئيسية" },
                    new Category { Name = "مقبلات" },
                    new Category { Name = "مشروبات" }
                };

                await _db.Categories.AddRangeAsync(categories);
                await _db.SaveChangesAsync();

                // Seed Products
                if (!_db.Products.Any())
                {
                    var mainCategory = categories.First(c => c.Name == "وجبات رئيسية");
                    var sideCategory = categories.First(c => c.Name == "مقبلات");
                    var drinkCategory = categories.First(c => c.Name == "مشروبات");

                    await _db.Products.AddRangeAsync(
                        new Product
                        {
                            Name = "شاورما دجاج كبير",
                            BarCode = "10001",
                            Description = "شاورما دجاج لذيذة مع الثوم والمخلل",
                            CostPrice = 8.00m,
                            SalePrice = 15.00m,
                            CategoryId = mainCategory.Id
                        },
                        new Product
                        {
                            Name = "برجر لحم مضاعف",
                            BarCode = "10002",
                            Description = "برجر لحم بقري مشوي مع الجبن والخس والصلصة الخاصة",
                            CostPrice = 15.00m,
                            SalePrice = 25.00m,
                            CategoryId = mainCategory.Id
                        },
                        new Product
                        {
                            Name = "بطاطس مقلية عائلية",
                            BarCode = "20001",
                            Description = "بطاطس مقلية مقرمشة ومملحة",
                            CostPrice = 4.00m,
                            SalePrice = 10.00m,
                            CategoryId = sideCategory.Id
                        },
                        new Product
                        {
                            Name = "مياه معدنية",
                            BarCode = "30001",
                            Description = "مياه معدنية باردة 500 مل",
                            CostPrice = 0.50m,
                            SalePrice = 1.50m,
                            CategoryId = drinkCategory.Id
                        },
                        new Product
                        {
                            Name = "عصير برتقال طازج",
                            BarCode = "30002",
                            Description = "عصير برتقال طبيعي طازج بدون سكر مضاف",
                            CostPrice = 3.00m,
                            SalePrice = 8.00m,
                            CategoryId = drinkCategory.Id
                        }
                    );
                    await _db.SaveChangesAsync();
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SecureLegacyDefaultAccountAsync(
        string userName,
        string knownDefaultPassword,
        string? replacementPassword)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !await _userManager.CheckPasswordAsync(user, knownDefaultPassword))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(replacementPassword))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, replacementPassword);
            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The legacy '{userName}' password could not be replaced: " +
                    string.Join(" | ", resetResult.Errors.Select(error => error.Description)));
            }
            return;
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
    }
}
