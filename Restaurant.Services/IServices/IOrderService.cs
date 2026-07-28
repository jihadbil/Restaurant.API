using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(OrderCreateDto orderCreateDto);
    Task<bool> UpdateOrderAsync(OrderUpdateDto orderUpdateDto);
    Task<bool> DeleteOrderAsync(int id);
}
