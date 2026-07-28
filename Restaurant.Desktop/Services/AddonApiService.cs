using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class AddonApiService : IAddonApiService
    {
        private readonly ApiClient _apiClient;

        public AddonApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<AddonDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<AddonDto>>("api/addons");
        }

        public async Task<ApiResult<AddonDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<AddonDto>($"api/addons/{id}");
        }

        public async Task<ApiResult<AddonDto>> CreateAsync(AddonCreateDto dto)
        {
            return await _apiClient.PostAsync<AddonCreateDto, AddonDto>("api/addons", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, AddonUpdateDto dto)
        {
            return await _apiClient.PutAsync<AddonUpdateDto>($"api/addons/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/addons/{id}");
        }
    }
}
