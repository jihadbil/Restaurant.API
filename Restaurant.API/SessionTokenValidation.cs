using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Restaurant.Models;
using System.Security.Claims;

namespace Restaurant.API;

public static class SessionTokenValidation
{
    public const string StampClaim = "security_stamp";

    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue(StampClaim);
        var user = id == null ? null : await manager.FindByIdAsync(id);
        if (user == null || string.IsNullOrWhiteSpace(stamp)
            || !string.Equals(stamp, user.SecurityStamp, StringComparison.Ordinal)
            || await manager.IsLockedOutAsync(user))
            context.Fail("انتهت الجلسة أو تغيرت صلاحيات الحساب. أعد تسجيل الدخول.");
    }
}
