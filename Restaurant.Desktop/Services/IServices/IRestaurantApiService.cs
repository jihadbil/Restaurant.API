using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IRestaurantApiService
    {
        Task<ApiResult<List<RestaurantDto>>> GetAllAsync();
        Task<ApiResult<RestaurantDto>> GetByIdAsync(int id);
        Task<ApiResult<RestaurantDto>> CreateAsync(RestaurantCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, RestaurantUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
        Task<ApiResult<string>> UploadLogoAsync(string filePath);
    }
}
