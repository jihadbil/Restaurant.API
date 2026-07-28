using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class OrderApiService : IOrderApiService
    {
        private readonly ApiClient _apiClient;

        public OrderApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<OrderDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<OrderDto>>("api/orders");
        }

        public async Task<ApiResult<OrderDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<OrderDto>($"api/orders/{id}");
        }

        public async Task<ApiResult<OrderDto>> CreateAsync(OrderCreateDto dto)
        {
            return await _apiClient.PostAsync<OrderCreateDto, OrderDto>("api/orders", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, OrderUpdateDto dto)
        {
            return await _apiClient.PutAsync<OrderUpdateDto>($"api/orders/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/orders/{id}");
        }
    }
}
