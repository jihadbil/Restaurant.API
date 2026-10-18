using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<PagedResult<OrderDto>> GetOrdersPageAsync(OrderQueryDto query);
    Task<OrderDto?> GetByRequestAsync(string requestId, string userId);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(OrderCreateDto orderCreateDto, string userId, bool canApplyDiscount);
    Task<OrderDto> CheckoutAsync(OrderCheckoutCreateDto checkoutDto, string userId, bool canApplyDiscount);
    Task<bool> UpdateOrderAsync(
        OrderUpdateDto orderUpdateDto,
        string userId,
        bool canApplyDiscount,
        bool canVoidOrder);
    Task<bool> DeleteOrderAsync(int id);
}
