using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public DataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        RestaurantDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
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

            // Seed Admin User
            var adminUser = await _userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@restaurant.com",
                    EmailConfirmed = true,
                    RestaurantId = defaultRestaurant.Id
                };
                var result = await _userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, AppRoles.Admin);

                    // Seed initial permissions for admin
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
                        await _userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("Permission", permission));
                    }
                }
            }
            else if (adminUser.RestaurantId == null)
            {
                adminUser.RestaurantId = defaultRestaurant.Id;
                await _userManager.UpdateAsync(adminUser);
            }

            // Seed Cashier User
            var cashierUser = await _userManager.FindByNameAsync("cashier");
            if (cashierUser == null)
            {
                cashierUser = new ApplicationUser
                {
                    UserName = "cashier",
                    Email = "cashier@restaurant.com",
                    EmailConfirmed = true,
                    RestaurantId = defaultRestaurant.Id
                };
                var result = await _userManager.CreateAsync(cashierUser, "Cashier@123456");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(cashierUser, AppRoles.Cashier);

                    // Seed initial permissions for cashier
                    var cashierPermissions = new[] { 
                        "Permission.NewOrder", "Permission.Orders", 
                        "Permission.POS.VoidItem" 
                    };
                    foreach (var permission in cashierPermissions)
                    {
                        await _userManager.AddClaimAsync(cashierUser, new System.Security.Claims.Claim("Permission", permission));
                    }
                }
            }
            else if (cashierUser.RestaurantId == null)
            {
                cashierUser.RestaurantId = defaultRestaurant.Id;
                await _userManager.UpdateAsync(cashierUser);
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
}
