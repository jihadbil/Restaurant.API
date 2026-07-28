using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface ICategoryApiService
    {
        Task<ApiResult<List<CategoryDto>>> GetAllAsync();
        Task<ApiResult<CategoryDto>> GetByIdAsync(int id);
        Task<ApiResult<CategoryDto>> CreateAsync(CategoryCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, CategoryUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
