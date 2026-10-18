using Microsoft.AspNetCore.Authorization;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using System.Security.Claims;

namespace Restaurant.API;

public static class PermissionAuthorization
{
    public static void Configure(AuthorizationOptions options)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        foreach (var permission in AppPermissions.All)
            options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => AppPermissions.Has(context.User, permission)));
        options.AddPolicy("ReportsAccess", policy => policy.RequireAuthenticatedUser()
            .RequireAssertion(context => AppPermissions.Has(context.User, AppPermissions.Reports)));
        TreasuryAuthorization.Configure(options);
    }

    public static OrderDto ForViewer(OrderDto order, ClaimsPrincipal user)
    {
        if (!AppPermissions.Has(user, AppPermissions.Reports) && !AppPermissions.Has(user, AppPermissions.Dashboard))
        {
            order.Cost = 0;
            order.Profit = 0;
            foreach (var item in order.OrderItems) item.UnitCostPrice = 0;
        }
        return order;
    }
}
