using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Security.Claims;

namespace Restaurant.Services;

public class UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
    IMapper mapper, IUnitOfWork unitOfWork) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var result = new List<UserDto>();
        foreach (var user in await userManager.Users.ToListAsync()) result.Add(await ToDto(user));
        return result;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        return user == null ? null : await ToDto(user);
    }

    private async Task<UserDto> ToDto(ApplicationUser user)
    {
        var dto = mapper.Map<UserDto>(user);
        dto.Roles = await userManager.GetRolesAsync(user);
        dto.Permissions = AppPermissions.Normalize((await userManager.GetClaimsAsync(user))
            .Where(c => c.Type == "Permission").Select(c => c.Value), false);
        return dto;
    }

    public Task<bool> UpdateUserAsync(string id, UserUpdateDto dto) => Write(async () =>
    {
        var permissions = dto.Permissions == null ? null : AppPermissions.Normalize(dto.Permissions);
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;
        if (dto.Roles != null) await SetRoles(user, dto.Roles);
        user.UserName = dto.UserName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        Ensure(await userManager.UpdateAsync(user));
        if (permissions != null)
        {
            var previous = (await userManager.GetClaimsAsync(user)).Where(c => c.Type == "Permission").ToList();
            Ensure(await userManager.RemoveClaimsAsync(user, previous));
            Ensure(await userManager.AddClaimsAsync(user, permissions.Select(p => new Claim("Permission", p))));
        }
        Ensure(await userManager.UpdateSecurityStampAsync(user));
        return true;
    });

    public Task<bool> DeleteUserAsync(string id) => Write(async () =>
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;
        if (await userManager.IsInRoleAsync(user, AppRoles.Admin)
            && (await userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count <= 1) return false;
        if (await unitOfWork.Orders.Query().AnyAsync(o => o.UserId == id)
            || await unitOfWork.CashDrawerEntries.Query().AnyAsync(e => e.UserId == id)
            || await unitOfWork.CashShifts.Query().AnyAsync(s => s.UserId == id)) return false;
        Ensure(await userManager.DeleteAsync(user));
        return true;
    });

    public async Task<bool> ChangePasswordAsync(string id, UserChangePasswordDto dto)
    {
        var user = await userManager.FindByIdAsync(id);
        return user != null && (await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword)).Succeeded;
    }

    public async Task<bool> ResetPasswordAsync(string id, string password)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return (await userManager.ResetPasswordAsync(user, token, password)).Succeeded;
    }

    public Task<bool> UpdateUserRolesAsync(string id, IEnumerable<string> roles) => Write(async () =>
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;
        await SetRoles(user, roles);
        Ensure(await userManager.UpdateSecurityStampAsync(user));
        return true;
    });

    private async Task SetRoles(ApplicationUser user, IEnumerable<string> roles)
    {
        var requested = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var role in requested)
            if (!await roleManager.RoleExistsAsync(role)) throw new ArgumentException("الدور المحدد غير موجود.");
        var current = await userManager.GetRolesAsync(user);
        if (current.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase)
            && !requested.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase)
            && (await userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count <= 1)
            throw new IdentityWriteException();
        Ensure(await userManager.AddToRolesAsync(user, requested.Except(current, StringComparer.OrdinalIgnoreCase)));
        Ensure(await userManager.RemoveFromRolesAsync(user, current.Except(requested, StringComparer.OrdinalIgnoreCase)));
    }

    private async Task<bool> Write(Func<Task<bool>> operation)
    {
        try { return await unitOfWork.ExecuteInTransactionAsync(operation); }
        catch (IdentityWriteException) { return false; }
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new IdentityWriteException();
    }
    private sealed class IdentityWriteException : Exception;

    public async Task<IEnumerable<string>> GetAvailableRolesAsync() =>
        await roleManager.Roles.Select(r => r.Name ?? string.Empty).ToListAsync();
}
