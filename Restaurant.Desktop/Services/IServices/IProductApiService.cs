using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IProductApiService
    {
        Task<ApiResult<List<ProductDto>>> GetAllAsync();
        Task<ApiResult<ProductDto>> GetByIdAsync(int id);
        Task<ApiResult<ProductDto>> CreateAsync(ProductCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, ProductUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
        Task<ApiResult<string>> UploadImageAsync(string filePath);
    }
}
