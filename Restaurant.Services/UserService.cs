using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
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
            var permissionClaims = currentClaims.Where(c => c.Type == "Permission");

            // Remove existing permission claims
            foreach (var claim in permissionClaims)
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            // Add new ones
            foreach (var perm in userUpdateDto.Permissions)
            {
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("Permission", perm));
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

        if (await _userManager.HasPasswordAsync(user))
        {
            await _userManager.RemovePasswordAsync(user);
        }
        var result = await _userManager.AddPasswordAsync(user, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserRolesAsync(string id, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return false;
        }

        // Filter valid roles that exist in the system to prevent exceptions
        var validRoles = new List<string>();
        foreach (var role in roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                validRoles.Add(role);
            }
        }

        if (validRoles.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, validRoles);
            return addResult.Succeeded;
        }

        return true;
    }

    public async Task<IEnumerable<string>> GetAvailableRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name ?? string.Empty).ToListAsync();
    }
}
