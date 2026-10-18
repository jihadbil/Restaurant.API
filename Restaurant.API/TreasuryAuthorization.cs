using Microsoft.AspNetCore.Authorization;
using Restaurant.Models;
using System.Security.Claims;

namespace Restaurant.API;

public static class TreasuryAuthorization
{
    public static bool CanViewTotals(ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin)
        || user.HasClaim("Permission", "Permission.Treasury") || user.HasClaim("Permission", "Permission.Shift.ViewTotals");

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy("TreasuryAccess", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            context.User.IsInRole(AppRoles.Admin) || context.User.HasClaim("Permission", "Permission.Treasury")));
        options.AddPolicy("CashTotals", policy => policy.RequireAuthenticatedUser().RequireAssertion(context => CanViewTotals(context.User)));
        options.AddPolicy("ShiftAccess", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            context.User.IsInRole(AppRoles.Admin) || context.User.HasClaim("Permission", "Permission.Treasury")
            || context.User.HasClaim("Permission", "Permission.NewOrder")));
    }
}
