using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class AuthApiService : IAuthApiService
    {
        private readonly ApiClient _apiClient;

        public AuthApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            return await _apiClient.PostAsync<LoginRequestDto, AuthResponseDto>("api/auth/login", dto);
        }

        public async Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto)
        {
            return await _apiClient.PostAsync<RegisterRequestDto, AuthResponseDto>("api/auth/register", dto);
        }
    }
}
