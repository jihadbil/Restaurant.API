using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IOrderApiService
    {
        Task<ApiResult<List<OrderDto>>> GetAllAsync();
        Task<ApiResult<OrderDto>> GetByIdAsync(int id);
        Task<ApiResult<OrderDto>> CreateAsync(OrderCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, OrderUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
