using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class CategoryApiService : ICategoryApiService
    {
        private readonly ApiClient _apiClient;

        public CategoryApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<CategoryDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<CategoryDto>>("api/categories");
        }

        public async Task<ApiResult<CategoryDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<CategoryDto>($"api/categories/{id}");
        }

        public async Task<ApiResult<CategoryDto>> CreateAsync(CategoryCreateDto dto)
        {
            return await _apiClient.PostAsync<CategoryCreateDto, CategoryDto>("api/categories", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            return await _apiClient.PutAsync<CategoryUpdateDto>($"api/categories/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/categories/{id}");
        }
    }
}
