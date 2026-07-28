using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<bool> UpdateUserAsync(string id, UserUpdateDto userUpdateDto);
    Task<bool> DeleteUserAsync(string id);
    Task<bool> ChangePasswordAsync(string id, UserChangePasswordDto changePasswordDto);
    Task<bool> ResetPasswordAsync(string id, string newPassword);
    Task<bool> UpdateUserRolesAsync(string id, IEnumerable<string> roles);
    Task<IEnumerable<string>> GetAvailableRolesAsync();
}
