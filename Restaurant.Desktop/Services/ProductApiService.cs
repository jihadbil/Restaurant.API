using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class ProductApiService : IProductApiService
    {
        private readonly ApiClient _apiClient;

        public ProductApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<ProductDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<ProductDto>>("api/products");
        }

        public async Task<ApiResult<ProductDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<ProductDto>($"api/products/{id}");
        }

        public async Task<ApiResult<ProductDto>> CreateAsync(ProductCreateDto dto)
        {
            return await _apiClient.PostAsync<ProductCreateDto, ProductDto>("api/products", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, ProductUpdateDto dto)
        {
            return await _apiClient.PutAsync<ProductUpdateDto>($"api/products/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/products/{id}");
        }

        public async Task<ApiResult<string>> UploadImageAsync(string filePath)
        {
            var result = await _apiClient.PostFileAsync<ImageUploadResponse>("api/products/upload-image", filePath);
            if (result.IsSuccess && result.Data != null)
            {
                return ApiResult<string>.Success(result.Data.ImageUrl, result.StatusCode);
            }
            return ApiResult<string>.Failure(result.ErrorMessage ?? "فشل رفع الصورة", result.StatusCode);
        }
    }
}
