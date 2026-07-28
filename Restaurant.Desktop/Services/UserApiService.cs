using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class UserApiService : IUserApiService
    {
        private readonly ApiClient _apiClient;

        public UserApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<IEnumerable<UserDto>>> GetUsersAsync()
        {
            return await _apiClient.GetAsync<IEnumerable<UserDto>>("api/users");
        }

        public async Task<ApiResult<IEnumerable<string>>> GetAvailableRolesAsync()
        {
            return await _apiClient.GetAsync<IEnumerable<string>>("api/users/roles");
        }

        public async Task<ApiResult<bool>> UpdateUserAsync(string id, UserUpdateDto userUpdateDto)
        {
            return await _apiClient.PutAsync($"api/users/{id}", userUpdateDto);
        }

        public async Task<ApiResult<bool>> UpdateUserRolesAsync(string id, IEnumerable<string> roles)
        {
            var roleUpdateDto = new UserRoleUpdateDto { Roles = roles };
            return await _apiClient.PostNoContentAsync($"api/users/{id}/roles", roleUpdateDto);
        }

        public async Task<ApiResult<bool>> ResetPasswordAsync(string id, UserResetPasswordDto resetPasswordDto)
        {
            return await _apiClient.PostNoContentAsync($"api/users/{id}/reset-password", resetPasswordDto);
        }

        public async Task<ApiResult<bool>> DeleteUserAsync(string id)
        {
            return await _apiClient.DeleteAsync($"api/users/{id}");
        }

        public async Task<ApiResult<AuthResponseDto>> RegisterUserAsync(RegisterRequestDto registerRequestDto)
        {
            return await _apiClient.PostAsync<RegisterRequestDto, AuthResponseDto>("api/auth/register", registerRequestDto);
        }
    }
}
