using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IAddonApiService
    {
        Task<ApiResult<List<AddonDto>>> GetAllAsync();
        Task<ApiResult<AddonDto>> GetByIdAsync(int id);
        Task<ApiResult<AddonDto>> CreateAsync(AddonCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, AddonUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
