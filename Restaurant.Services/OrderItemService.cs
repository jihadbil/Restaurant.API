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
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == orderId,
            includeProperties: "OrderItems,CashDrawerEntries");
        if (order == null)
        {
            return null;
        }

        EnsureOrderIsEditable(order);

        var product = await _unitOfWork.Products.GetFirstOrDefaultAsync(
            product => product.Id == orderItemCreateDto.ProductId, includeProperties: "Category");
        if (product == null || !product.IsAvailable)
        {
            throw new ArgumentException("المنتج المحدد غير موجود.");
        }

        if (orderItemCreateDto.UnitDiscount < 0 || orderItemCreateDto.UnitDiscount > product.SalePrice)
        {
            throw new ArgumentException("قيمة خصم المنتج غير صالحة.");
        }

        var orderItem = new OrderItem
        {
            OrderId = orderId,
            ProductId = product.Id,
            Quantity = orderItemCreateDto.Quantity,
            UnitSalePrice = product.SalePrice,
            UnitCostPrice = product.CostPrice,
            UnitDiscount = orderItemCreateDto.UnitDiscount,
            Notes = orderItemCreateDto.Notes
        };
        orderItem.Total = orderItem.Quantity * (orderItem.UnitSalePrice - orderItem.UnitDiscount);

        OrderAccounting.Snapshot(orderItem, product);
        order.OrderItems.Add(orderItem);
        RecalculateOrderTotals(order);
        await _unitOfWork.SaveAsync();

        var savedItem = await _unitOfWork.OrderItems.GetFirstOrDefaultAsync(
            i => i.Id == orderItem.Id,
            includeProperties: "Product"
        );
        return _mapper.Map<OrderItemDto>(savedItem);
    }

    public async Task<bool> UpdateOrderItemAsync(OrderItemUpdateDto orderItemUpdateDto)
    {
        var orderItem = await _unitOfWork.OrderItems.GetFirstOrDefaultAsync(
            i => i.Id == orderItemUpdateDto.Id
        );
        if (orderItem == null)
        {
            return false;
        }

        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == orderItem.OrderId,
            includeProperties: "OrderItems,CashDrawerEntries");
        if (order == null)
        {
            return false;
        }

        EnsureOrderIsEditable(order);

        var product = await _unitOfWork.Products.GetFirstOrDefaultAsync(
            product => product.Id == orderItemUpdateDto.ProductId, includeProperties: "Category");
        if (product == null || !product.IsAvailable)
        {
            throw new ArgumentException("المنتج المحدد غير موجود.");
        }

        if (orderItemUpdateDto.UnitDiscount < 0 || orderItemUpdateDto.UnitDiscount > product.SalePrice)
        {
            throw new ArgumentException("قيمة خصم المنتج غير صالحة.");
        }

        orderItem.ProductId = product.Id;
        orderItem.Quantity = orderItemUpdateDto.Quantity;
        orderItem.UnitSalePrice = product.SalePrice;
        orderItem.UnitCostPrice = product.CostPrice;
        orderItem.UnitDiscount = orderItemUpdateDto.UnitDiscount;
        orderItem.Notes = orderItemUpdateDto.Notes;
        OrderAccounting.Snapshot(orderItem, product);
        orderItem.Total = orderItem.Quantity * (orderItem.UnitSalePrice - orderItem.UnitDiscount);

        RecalculateOrderTotals(order);
        await _unitOfWork.SaveAsync();

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
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            existingOrder => existingOrder.Id == orderId,
            includeProperties: "OrderItems,CashDrawerEntries");
        if (order == null)
        {
            return false;
        }

        EnsureOrderIsEditable(order);
        _unitOfWork.OrderItems.Remove(orderItem);
        order.OrderItems.Remove(orderItem);
        RecalculateOrderTotals(order);
        await _unitOfWork.SaveAsync();

        return true;
    }

    private static void EnsureOrderIsEditable(Order order)
    {
        if (order.CashDrawerEntries.Count > 0)
        {
            throw new InvalidOperationException("لا يمكن تعديل عناصر طلب له حركات مالية.");
        }

        if (order.OrderStatus == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("لا يمكن تعديل عناصر طلب ملغى.");
        }
    }

    private static void RecalculateOrderTotals(Order order)
    {
        OrderAccounting.Recalculate(order);
        var itemTotal = order.OrderItems.Sum(item => item.Total);
        if (order.Discount > itemTotal)
        {
            throw new InvalidOperationException("خصم الطلب يتجاوز إجمالي عناصره بعد التعديل.");
        }

        order.Cost = order.OrderItems.Sum(item => item.Quantity * item.UnitCostPrice);
        order.Total = itemTotal - order.Discount;
        order.Profit = order.Total - order.Cost;
    }
}
