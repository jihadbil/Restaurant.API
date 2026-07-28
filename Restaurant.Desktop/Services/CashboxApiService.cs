using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class CashboxApiService : ICashboxApiService
    {
        private readonly ApiClient _apiClient;

        public CashboxApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<CashboxDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<CashboxDto>>("api/cashboxes");
        }

        public async Task<ApiResult<CashboxBalanceDto>> GetBalanceAsync(int id)
        {
            return await _apiClient.GetAsync<CashboxBalanceDto>($"api/cashboxes/{id}/balance");
        }

        public async Task<ApiResult<CashboxDto>> CreateAsync(CashboxCreateDto dto)
        {
            return await _apiClient.PostAsync<CashboxCreateDto, CashboxDto>("api/cashboxes", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, CashboxUpdateDto dto)
        {
            return await _apiClient.PutAsync<CashboxUpdateDto>($"api/cashboxes/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/cashboxes/{id}");
        }
    }
}
