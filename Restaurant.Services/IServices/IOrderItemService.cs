using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IOrderItemService
{
    Task<IEnumerable<OrderItemDto>> GetAllOrderItemsAsync();
    Task<IEnumerable<OrderItemDto>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<OrderItemDto?> GetOrderItemByIdAsync(int id);
    Task<OrderItemDto?> CreateOrderItemAsync(int orderId, OrderItemCreateDto orderItemCreateDto);
    Task<bool> UpdateOrderItemAsync(OrderItemUpdateDto orderItemUpdateDto);
    Task<bool> DeleteOrderItemAsync(int id);
}
