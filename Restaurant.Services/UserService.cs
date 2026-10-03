using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var dto = _mapper.Map<UserDto>(user);
            dto.Roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            dto.Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
            userDtos.Add(dto);
        }

        return userDtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        var dto = _mapper.Map<UserDto>(user);
        dto.Roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        dto.Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
        return dto;
    }

    public async Task<bool> UpdateUserAsync(string id, UserUpdateDto userUpdateDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.UserName = userUpdateDto.UserName;
        user.Email = userUpdateDto.Email;
        user.PhoneNumber = userUpdateDto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return false;
        }

        // Update permissions claims
        if (userUpdateDto.Permissions != null)
        {
            var currentClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = currentClaims.Where(c => c.Type == "Permission").ToList();
            var requestedPermissions = userUpdateDto.Permissions
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var addedClaims = new List<System.Security.Claims.Claim>();
            foreach (var permission in requestedPermissions.Except(
                         permissionClaims.Select(claim => claim.Value),
                         StringComparer.Ordinal))
            {
                var claim = new System.Security.Claims.Claim("Permission", permission);
                var addResult = await _userManager.AddClaimAsync(user, claim);
                if (!addResult.Succeeded)
                {
                    foreach (var addedClaim in addedClaims)
                    {
                        await _userManager.RemoveClaimAsync(user, addedClaim);
                    }
                    return false;
                }
                addedClaims.Add(claim);
            }

            foreach (var claim in permissionClaims.Where(claim =>
                         !requestedPermissions.Contains(claim.Value, StringComparer.Ordinal)))
            {
                var removeResult = await _userManager.RemoveClaimAsync(user, claim);
                if (!removeResult.Succeeded)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            var administrators = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (administrators.Count <= 1)
            {
                return false;
            }
        }

        var hasOrders = (await _unitOfWork.Orders.GetAllAsync(order => order.UserId == id)).Any();
        var hasCashEntries = (await _unitOfWork.CashDrawerEntries.GetAllAsync(entry => entry.UserId == id)).Any();
        if (hasOrders || hasCashEntries)
        {
            return false;
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(string id, UserChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        return result.Succeeded;
    }

    public async Task<bool> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserRolesAsync(string id, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        var requestedRoles = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var role in requestedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return false;
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase)
            && !requestedRoles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            var administrators = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (administrators.Count <= 1)
            {
                return false;
            }
        }
        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToList();

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return false;
            }
        }

        if (rolesToRemove.Count == 0)
        {
            return true;
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        if (!removeResult.Succeeded && rolesToAdd.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToAdd);
        }

        return removeResult.Succeeded;
    }

    public async Task<IEnumerable<string>> GetAvailableRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name ?? string.Empty).ToListAsync();
    }
}
