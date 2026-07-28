using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class RestaurantApiService : IRestaurantApiService
    {
        private readonly ApiClient _apiClient;

        public RestaurantApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<RestaurantDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<RestaurantDto>>("api/restaurants");
        }

        public async Task<ApiResult<RestaurantDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<RestaurantDto>($"api/restaurants/{id}");
        }

        public async Task<ApiResult<RestaurantDto>> CreateAsync(RestaurantCreateDto dto)
        {
            return await _apiClient.PostAsync<RestaurantCreateDto, RestaurantDto>("api/restaurants", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, RestaurantUpdateDto dto)
        {
            return await _apiClient.PutAsync<RestaurantUpdateDto>($"api/restaurants/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/restaurants/{id}");
        }

        public async Task<ApiResult<string>> UploadLogoAsync(string filePath)
        {
            var result = await _apiClient.PostFileAsync<ImageUploadResponse>("api/restaurants/upload-logo", filePath);
            if (result.IsSuccess && result.Data != null)
            {
                return ApiResult<string>.Success(result.Data.ImageUrl, result.StatusCode);
            }
            return ApiResult<string>.Failure(result.ErrorMessage ?? "فشل رفع الشعار", result.StatusCode);
        }
    }
}
