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

    public async Task<OrderDto> CreateOrderAsync(
        OrderCreateDto orderCreateDto,
        string userId,
        bool canApplyDiscount)
    {
        var order = await BuildOrderAsync(orderCreateDto, userId, canApplyDiscount);

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveAsync();

        return await LoadOrderAsync(order.Id);
    }

    public async Task<OrderDto> CheckoutAsync(
        OrderCheckoutCreateDto checkoutDto,
        string userId,
        bool canApplyDiscount)
    {
        var requestId = checkoutDto.ClientRequestId.Trim();
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("معرف عملية الدفع مطلوب.");
        }

        var existingOrder = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            order => order.ClientRequestId == requestId && order.UserId == userId,
            includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox",
            tracked: false);
        if (existingOrder != null)
        {
            return _mapper.Map<OrderDto>(existingOrder);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var duplicateOrder = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
                order => order.ClientRequestId == requestId && order.UserId == userId,
                includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox");
            if (duplicateOrder != null)
            {
                return _mapper.Map<OrderDto>(duplicateOrder);
            }

            var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(
                cashbox => cashbox.Id == checkoutDto.CashboxId);
            if (cashbox == null || !cashbox.IsActive)
            {
                throw new ArgumentException("الخزينة المحددة غير موجودة أو غير نشطة.");
            }

            var paymentMethod = await _unitOfWork.PaymentMethods.GetFirstOrDefaultAsync(
                method => method.Id == checkoutDto.PaymentMethodId);
            if (paymentMethod == null)
            {
                throw new ArgumentException("طريقة الدفع المحددة غير موجودة.");
            }

            var order = await BuildOrderAsync(
                checkoutDto.Order,
                userId,
                canApplyDiscount,
                requestId);
            if (order.Total <= 0)
            {
                throw new ArgumentException("إجمالي الطلب بعد الخصم يجب أن يكون أكبر من صفر.");
            }

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveAsync();

            await _unitOfWork.CashDrawerEntries.AddAsync(new CashDrawerEntry
            {
                Date = DateTime.Now,
                Amount = order.Total,
                EntryType = CashDrawerEntryType.SalePayment,
                Notes = $"دفعة الطلب رقم {order.OrderNumber}",
                CashboxId = checkoutDto.CashboxId,
                PaymentMethodId = checkoutDto.PaymentMethodId,
                OrderId = order.Id,
                UserId = userId
            });
            await _unitOfWork.SaveAsync();

            return await LoadOrderAsync(order.Id);
        });
    }

    private async Task<OrderDto> LoadOrderAsync(int orderId)
    {
        var savedOrder = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            order => order.Id == orderId,
            includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox");

        return _mapper.Map<OrderDto>(savedOrder);
    }

    private async Task<Order> BuildOrderAsync(
        OrderCreateDto orderCreateDto,
        string userId,
        bool canApplyDiscount,
        string? clientRequestId = null)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || await _unitOfWork.ApplicationUsers.GetFirstOrDefaultAsync(user => user.Id == userId) == null)
        {
            throw new ArgumentException("المستخدم الذي ينشئ الطلب غير موجود.");
        }

        if (orderCreateDto.OrderItems == null || orderCreateDto.OrderItems.Count == 0)
        {
            throw new ArgumentException("يجب إضافة عنصر واحد على الأقل للطلب.");
        }

        if (orderCreateDto.Discount < 0)
        {
            throw new ArgumentException("قيمة الخصم لا يمكن أن تكون سالبة.");
        }

        if (!canApplyDiscount
            && (orderCreateDto.Discount > 0 || orderCreateDto.OrderItems.Any(item => item.UnitDiscount > 0)))
        {
            throw new UnauthorizedAccessException("المستخدم لا يملك صلاحية تطبيق الخصم.");
        }

        var productIds = orderCreateDto.OrderItems.Select(item => item.ProductId).Distinct().ToArray();
        var products = (await _unitOfWork.Products.GetAllAsync(product => productIds.Contains(product.Id)))
            .ToDictionary(product => product.Id);
        if (products.Count != productIds.Length)
        {
            throw new ArgumentException("أحد المنتجات المحددة غير موجود.");
        }

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            ClientRequestId = clientRequestId,
            Date = DateTime.Now,
            Discount = orderCreateDto.Discount,
            OrderStatus = orderCreateDto.OrderStatus is OrderStatus.Preparing or OrderStatus.Ready
                ? orderCreateDto.OrderStatus
                : OrderStatus.Preparing,
            OrderType = orderCreateDto.OrderType,
            Notes = orderCreateDto.Notes,
            UserId = userId
        };

        foreach (var itemDto in orderCreateDto.OrderItems)
        {
            if (itemDto.Quantity <= 0)
            {
                throw new ArgumentException("كمية المنتج يجب أن تكون أكبر من صفر.");
            }

            var product = products[itemDto.ProductId];
            if (itemDto.UnitDiscount < 0 || itemDto.UnitDiscount > product.SalePrice)
            {
                throw new ArgumentException($"خصم المنتج '{product.Name}' غير صالح.");
            }

            var item = new OrderItem
            {
                Quantity = itemDto.Quantity,
                UnitSalePrice = product.SalePrice,
                UnitCostPrice = product.CostPrice,
                UnitDiscount = itemDto.UnitDiscount,
                Notes = itemDto.Notes,
                ProductId = product.Id
            };
            item.Total = item.Quantity * (item.UnitSalePrice - item.UnitDiscount);
            order.OrderItems.Add(item);
        }

        var itemTotal = order.OrderItems.Sum(item => item.Total);
        if (order.Discount > itemTotal)
        {
            throw new ArgumentException("قيمة الخصم تتجاوز إجمالي الطلب.");
        }

        order.Cost = order.OrderItems.Sum(item => item.Quantity * item.UnitCostPrice);
        order.Total = itemTotal - order.Discount;
        order.Profit = order.Total - order.Cost;
        return order;
    }

    private async Task<int> GenerateOrderNumberAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var orderNumber = Random.Shared.Next(100_000, int.MaxValue);
            if (await _unitOfWork.Orders.GetFirstOrDefaultAsync(
                    order => order.OrderNumber == orderNumber,
                    tracked: false) == null)
            {
                return orderNumber;
            }
        }

        throw new InvalidOperationException("تعذر توليد رقم طلب فريد. يرجى إعادة المحاولة.");
    }

    public async Task<bool> UpdateOrderAsync(
        OrderUpdateDto orderUpdateDto,
        string userId,
        bool canApplyDiscount,
        bool canVoidOrder)
    {
        // Fetch order with items to recalculate totals if discount changes
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == orderUpdateDto.Id,
            includeProperties: "OrderItems,CashDrawerEntries"
        );

        if (order == null)
        {
            return false;
        }

        var discountChanged = orderUpdateDto.Discount != order.Discount;
        if (discountChanged && !canApplyDiscount)
        {
            throw new UnauthorizedAccessException("المستخدم لا يملك صلاحية تطبيق الخصم.");
        }

        if (discountChanged && order.CashDrawerEntries.Count > 0)
        {
            throw new InvalidOperationException("لا يمكن تغيير خصم طلب بعد تسجيل دفعة مالية عليه.");
        }

        var wasCancelled = order.OrderStatus == OrderStatus.Cancelled;
        var isCancelling = !wasCancelled && orderUpdateDto.OrderStatus == OrderStatus.Cancelled;
        if (wasCancelled && orderUpdateDto.OrderStatus != OrderStatus.Cancelled)
        {
            throw new ArgumentException("لا يمكن إعادة فتح طلب ملغى.");
        }

        if (isCancelling && !canVoidOrder)
        {
            throw new UnauthorizedAccessException("المستخدم لا يملك صلاحية إلغاء الطلب.");
        }

        // Apply changes from DTO
        _mapper.Map(orderUpdateDto, order);

        // Recalculate totals
        decimal totalItemsAmount = 0;
        foreach (var item in order.OrderItems)
        {
            totalItemsAmount += item.Total;
        }

        if (order.Discount < 0 || order.Discount > totalItemsAmount)
        {
            throw new ArgumentException("قيمة الخصم غير صالحة.");
        }

        order.Total = totalItemsAmount - order.Discount;
        order.Profit = order.Total - order.Cost;

        if (isCancelling)
        {
            foreach (var payment in order.CashDrawerEntries
                         .Where(entry => entry.EntryType == CashDrawerEntryType.SalePayment)
                         .ToList())
            {
                await _unitOfWork.CashDrawerEntries.AddAsync(new CashDrawerEntry
                {
                    Date = DateTime.Now,
                    Amount = payment.Amount,
                    EntryType = CashDrawerEntryType.Refund,
                    Notes = $"استرداد دفعة الطلب رقم {order.OrderNumber}",
                    CashboxId = payment.CashboxId,
                    PaymentMethodId = payment.PaymentMethodId,
                    OrderId = order.Id,
                    UserId = userId
                });
            }
        }

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveAsync();
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
            o => o.Id == id,
            includeProperties: "CashDrawerEntries");
        if (order == null)
        {
            return false;
        }

        if (order.CashDrawerEntries.Count > 0)
        {
            throw new InvalidOperationException("لا يمكن حذف طلب له حركات مالية. استخدم إلغاء الطلب بدلاً من ذلك.");
        }

        _unitOfWork.Orders.Remove(order);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
