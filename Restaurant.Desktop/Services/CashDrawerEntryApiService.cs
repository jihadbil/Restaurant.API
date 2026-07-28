using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class CashDrawerEntryApiService : ICashDrawerEntryApiService
    {
        private readonly ApiClient _apiClient;

        public CashDrawerEntryApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<CashDrawerEntryDto>> CreateAsync(CashDrawerEntryCreateDto dto)
        {
            return await _apiClient.PostAsync<CashDrawerEntryCreateDto, CashDrawerEntryDto>("api/cashdrawerentries", dto);
        }

        public async Task<ApiResult<List<CashDrawerEntryDto>>> GetAllAsync(int? cashboxId, DateTime? from, DateTime? to)
        {
            var endpoint = "api/cashdrawerentries";
            var queryParams = new List<string>();

            if (cashboxId.HasValue)
            {
                queryParams.Add($"cashboxId={cashboxId.Value}");
            }
            if (from.HasValue)
            {
                queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
            }
            if (to.HasValue)
            {
                queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
            }

            if (queryParams.Count > 0)
            {
                endpoint += "?" + string.Join("&", queryParams);
            }

            return await _apiClient.GetAsync<List<CashDrawerEntryDto>>(endpoint);
        }

        public async Task<ApiResult<List<CashDrawerEntryDto>>> GetByOrderAsync(int orderId)
        {
            return await _apiClient.GetAsync<List<CashDrawerEntryDto>>($"api/cashdrawerentries/byorder/{orderId}");
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/cashdrawerentries/{id}");
        }
    }
}
