using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _unitOfWork.Orders.GetAllAsync(
            includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox"
        );
        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == id,
            includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox"
        );
        return _mapper.Map<OrderDto?>(order);
    }

    public async Task<OrderDto> CreateOrderAsync(OrderCreateDto orderCreateDto)
    {
        var order = _mapper.Map<Order>(orderCreateDto);
        order.Date = DateTime.Now;
        order.OrderStatus ??= OrderStatus.Ready;

        // Calculate OrderItem Totals
        decimal totalItemsAmount = 0;
        decimal totalCost = 0;

        foreach (var item in order.OrderItems)
        {
            item.Total = item.Quantity * (item.UnitSalePrice - item.UnitDiscount);
            totalItemsAmount += item.Total;
            totalCost += item.Quantity * item.UnitCostPrice;
        }

        // Calculate Order Totals
        order.Cost = totalCost;
        order.Total = totalItemsAmount - order.Discount;
        order.Profit = order.Total - order.Cost;

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveAsync();

        // Fetch saved order with all relations to map to DTO properly
        var savedOrder = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == order.Id,
            includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox"
        );

        return _mapper.Map<OrderDto>(savedOrder);
    }

    public async Task<bool> UpdateOrderAsync(OrderUpdateDto orderUpdateDto)
    {
        // Fetch order with items to recalculate totals if discount changes
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == orderUpdateDto.Id,
            includeProperties: "OrderItems"
        );

        if (order == null)
        {
            return false;
        }

        // Apply changes from DTO
        _mapper.Map(orderUpdateDto, order);

        // Recalculate totals
        decimal totalItemsAmount = 0;
        foreach (var item in order.OrderItems)
        {
            totalItemsAmount += item.Total;
        }

        order.Total = totalItemsAmount - order.Discount;
        order.Profit = order.Total - order.Cost;

        _unitOfWork.Orders.Update(order);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return false;
        }

        _unitOfWork.Orders.Remove(order);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
