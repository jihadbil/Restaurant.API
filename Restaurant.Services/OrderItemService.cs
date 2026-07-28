using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class OrderItemService : IOrderItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OrderItemService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderItemDto>> GetAllOrderItemsAsync()
    {
        var orderItems = await _unitOfWork.OrderItems.GetAllAsync(includeProperties: "Product");
        return _mapper.Map<IEnumerable<OrderItemDto>>(orderItems);
    }

    public async Task<IEnumerable<OrderItemDto>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        var orderItems = await _unitOfWork.OrderItems.GetAllAsync(
            i => i.OrderId == orderId,
            includeProperties: "Product"
        );
        return _mapper.Map<IEnumerable<OrderItemDto>>(orderItems);
    }

    public async Task<OrderItemDto?> GetOrderItemByIdAsync(int id)
    {
        var orderItem = await _unitOfWork.OrderItems.GetFirstOrDefaultAsync(
            i => i.Id == id,
            includeProperties: "Product"
        );
        return _mapper.Map<OrderItemDto?>(orderItem);
    }

    public async Task<OrderItemDto?> CreateOrderItemAsync(int orderId, OrderItemCreateDto orderItemCreateDto)
    {
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            return null;
        }

        var orderItem = _mapper.Map<OrderItem>(orderItemCreateDto);
        orderItem.OrderId = orderId;
        orderItem.Total = orderItem.Quantity * (orderItem.UnitSalePrice - orderItem.UnitDiscount);

        await _unitOfWork.OrderItems.AddAsync(orderItem);
        await _unitOfWork.SaveAsync();

        // Recalculate order totals
        await RecalculateOrderTotalsAsync(orderId);

        var savedItem = await _unitOfWork.OrderItems.GetFirstOrDefaultAsync(
            i => i.Id == orderItem.Id,
            includeProperties: "Product"
        );
        return _mapper.Map<OrderItemDto>(savedItem);
    }

    public async Task<bool> UpdateOrderItemAsync(OrderItemUpdateDto orderItemUpdateDto)
    {
        var orderItem = await _unitOfWork.OrderItems.GetFirstOrDefaultAsync(
            i => i.Id == orderItemUpdateDto.Id,
            tracked: false
        );
        if (orderItem == null)
        {
            return false;
        }

        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(o => o.Id == orderItem.OrderId);
        if (order == null)
        {
            return false;
        }

        _mapper.Map(orderItemUpdateDto, orderItem);
        orderItem.Total = orderItem.Quantity * (orderItem.UnitSalePrice - orderItem.UnitDiscount);

        _unitOfWork.OrderItems.Update(orderItem);
        await _unitOfWork.SaveAsync();

        // Recalculate order totals
        await RecalculateOrderTotalsAsync(orderItem.OrderId);

        return true;
    }

    public async Task<bool> DeleteOrderItemAsync(int id)
    {
        var orderItem = await _unitOfWork.OrderItems.GetFirstOrDefaultAsync(i => i.Id == id);
        if (orderItem == null)
        {
            return false;
        }

        int orderId = orderItem.OrderId;
        _unitOfWork.OrderItems.Remove(orderItem);
        await _unitOfWork.SaveAsync();

        // Recalculate order totals
        await RecalculateOrderTotalsAsync(orderId);

        return true;
    }

    private async Task RecalculateOrderTotalsAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            return;
        }

        var orderItems = await _unitOfWork.OrderItems.GetAllAsync(i => i.OrderId == orderId);

        decimal totalItemsAmount = 0;
        decimal totalCost = 0;

        foreach (var item in orderItems)
        {
            totalItemsAmount += item.Total;
            totalCost += item.Quantity * item.UnitCostPrice;
        }

        order.Cost = totalCost;
        order.Total = totalItemsAmount - order.Discount;
        order.Profit = order.Total - order.Cost;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveAsync();
    }
}
