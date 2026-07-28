using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IUserApiService
    {
        Task<ApiResult<IEnumerable<UserDto>>> GetUsersAsync();
        Task<ApiResult<IEnumerable<string>>> GetAvailableRolesAsync();
        Task<ApiResult<bool>> UpdateUserAsync(string id, UserUpdateDto userUpdateDto);
        Task<ApiResult<bool>> UpdateUserRolesAsync(string id, IEnumerable<string> roles);
        Task<ApiResult<bool>> ResetPasswordAsync(string id, UserResetPasswordDto resetPasswordDto);
        Task<ApiResult<bool>> DeleteUserAsync(string id);
        Task<ApiResult<AuthResponseDto>> RegisterUserAsync(RegisterRequestDto registerRequestDto);
    }
}
