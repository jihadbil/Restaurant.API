using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IAuthApiService
    {
        Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
        Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
    }
}
