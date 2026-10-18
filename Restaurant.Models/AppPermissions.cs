using System.Security.Claims;

namespace Restaurant.Models;

public static class AppPermissions
{
    public const string Dashboard = "Permission.Dashboard";
    public const string NewOrder = "Permission.NewOrder";
    public const string Orders = "Permission.Orders";
    public const string Products = "Permission.Products";
    public const string ManageProducts = "Permission.Products.Manage";
    public const string Categories = "Permission.Categories";
    public const string ManageCategories = "Permission.Categories.Manage";
    public const string Reports = "Permission.Reports";
    public const string Settings = "Permission.Settings";
    public const string Treasury = "Permission.Treasury";
    public const string ApplyDiscount = "Permission.POS.ApplyDiscount";
    public const string VoidItem = "Permission.POS.VoidItem";
    public const string VoidOrder = "Permission.Orders.VoidOrder";
    public const string ViewShiftTotals = "Permission.Shift.ViewTotals";
    public static readonly IReadOnlyList<string> All = [Dashboard, NewOrder, Orders, Products, ManageProducts,
        Categories, ManageCategories, Reports, Settings, Treasury, ApplyDiscount, VoidItem, VoidOrder, ViewShiftTotals];

    public static string? Canonical(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value is "POS" or "Permission.POS") return NewOrder;
        var canonical = value.StartsWith("Permission.", StringComparison.Ordinal) ? value : "Permission." + value;
        return All.Contains(canonical, StringComparer.Ordinal) ? canonical : null;
    }

    public static string[] Normalize(IEnumerable<string>? values, bool rejectUnknown = true) =>
        (values ?? []).Select(value => Canonical(value) ?? (rejectUnknown
            ? throw new ArgumentException("صلاحية غير معروفة: " + value) : null))
        .OfType<string>().Distinct(StringComparer.Ordinal).ToArray();

    public static bool Has(ClaimsPrincipal user, string permission) => user.Identity?.IsAuthenticated == true
        && (user.IsInRole(AppRoles.Admin) || user.HasClaim("Permission", permission)
            || permission == Products && user.HasClaim("Permission", ManageProducts)
            || permission == Categories && user.HasClaim("Permission", ManageCategories));
}
