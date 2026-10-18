using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync() =>
        (await GetOrdersPageAsync(new OrderQueryDto { PageSize = 200 })).Items;

    public async Task<PagedResult<OrderDto>> GetOrdersPageAsync(OrderQueryDto filter)
    {
        if (filter.Page < 1 || filter.Page > 100000 || filter.PageSize < 1 || filter.PageSize > 200)
            throw new ArgumentException("حدود الصفحة غير صالحة.");
        var query = _unitOfWork.Orders.Query();
        if (filter.From.HasValue) query = query.Where(o => o.Date >= filter.From.Value.Date);
        if (filter.To.HasValue)
        {
            var period = ReportPeriod.Create(filter.From ?? filter.To.Value, filter.To.Value);
            query = query.Where(o => o.Date < period.EndExclusive);
        }
        if (filter.Status.HasValue) query = query.Where(o => o.OrderStatus == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var text = filter.Search.Trim();
            var number = int.TryParse(text, out var n) ? n : -1;
            query = query.Where(o => o.OrderNumber == number || o.Notes.Contains(text) || o.User.UserName!.Contains(text));
        }
        var count = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.Id).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Include(o => o.User).Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.CashDrawerEntries).ThenInclude(e => e.PaymentMethod)
            .Include(o => o.CashDrawerEntries).ThenInclude(e => e.Cashbox).AsSplitQuery().ToListAsync();
        return new(_mapper.Map<List<OrderDto>>(orders), count, filter.Page, filter.PageSize);
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
        ValidateItems(checkoutDto.Order);
        var requestId = checkoutDto.ClientRequestId?.Trim() ?? string.Empty;
        var fingerprint = Fingerprint(checkoutDto);
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
            EnsureSameRequest(existingOrder, checkoutDto, fingerprint);
            return _mapper.Map<OrderDto>(existingOrder);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var duplicateOrder = await _unitOfWork.Orders.GetFirstOrDefaultAsync(
                order => order.ClientRequestId == requestId && order.UserId == userId,
                includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox");
            if (duplicateOrder != null)
            {
                EnsureSameRequest(duplicateOrder, checkoutDto, fingerprint);
                return _mapper.Map<OrderDto>(duplicateOrder);
            }

            var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(
                cashbox => cashbox.Id == checkoutDto.CashboxId);
            if (cashbox == null || !cashbox.IsActive || !cashbox.IsDrawer)
            {
                throw new ArgumentException("اختر خزينة درج نشطة لنقطة البيع من الإعدادات.");
            }

            var paymentMethod = await _unitOfWork.PaymentMethods.GetFirstOrDefaultAsync(
                method => method.Id == checkoutDto.PaymentMethodId);
            if (paymentMethod == null || !paymentMethod.IsActive)
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
            CashShiftService.ValidateMoney(order.Total);
            order.RequestFingerprint = fingerprint;
            if (checkoutDto.ExpectedTotal.HasValue && checkoutDto.ExpectedTotal.Value != order.Total)
                throw new InvalidOperationException("تغيرت الأسعار. حدّث السلة وراجع المبلغ قبل تأكيد الدفع.");

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveAsync();

            await _unitOfWork.CashDrawerEntries.AddAsync(new CashDrawerEntry
            {
                Date = BusinessClock.Now,
                Amount = order.Total,
                EntryType = CashDrawerEntryType.SalePayment,
                Notes = $"دفعة الطلب رقم {order.OrderNumber}",
                CashboxId = checkoutDto.CashboxId,
                PaymentMethodId = checkoutDto.PaymentMethodId,
                PaymentMethodNameSnapshot = paymentMethod.Name,
                AffectsCash = paymentMethod.IsCash,
                OrderId = order.Id,
                UserId = userId
            });
            await _unitOfWork.SaveAsync();

            return await LoadOrderAsync(order.Id);
        });
    }

    public async Task<OrderDto?> GetByRequestAsync(string requestId, string userId)
    {
        var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(o => o.ClientRequestId == requestId && o.UserId == userId,
            includeProperties: "User,OrderItems,OrderItems.Product,CashDrawerEntries,CashDrawerEntries.PaymentMethod,CashDrawerEntries.Cashbox", tracked: false);
        return _mapper.Map<OrderDto?>(order);
    }

    private static string Fingerprint(OrderCheckoutCreateDto dto) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { dto.CashboxId, dto.PaymentMethodId, dto.ExpectedTotal,
            dto.Order.Discount, dto.Order.OrderType, dto.Order.Notes,
            Items = dto.Order.OrderItems.Select(i => new { i.ProductId, i.Quantity, i.UnitDiscount, i.Notes }).ToArray() }))));

    private static void EnsureSameRequest(Order order, OrderCheckoutCreateDto dto, string fingerprint)
    {
        if (order.RequestFingerprint != null)
        {
            if (order.RequestFingerprint != fingerprint) throw new InvalidOperationException("معرف الدفع مستخدم لطلب مختلف.");
            return;
        }
        // Compatibility with checkouts saved before fingerprints were introduced.
        var payment = order.CashDrawerEntries.FirstOrDefault(e => e.EntryType == CashDrawerEntryType.SalePayment);
        var sameItems = order.OrderItems.OrderBy(i => i.Id).Select(i => (i.ProductId, i.Quantity, i.UnitDiscount, i.Notes))
            .SequenceEqual(dto.Order.OrderItems.Select(i => (i.ProductId, i.Quantity, i.UnitDiscount, i.Notes)));
        if (payment?.CashboxId != dto.CashboxId || payment.PaymentMethodId != dto.PaymentMethodId
            || order.Discount != dto.Order.Discount || !sameItems)
            throw new InvalidOperationException("معرف الدفع مستخدم لطلب مختلف.");
    }

    private static void ValidateItems(OrderCreateDto? order)
    {
        if (order?.OrderItems == null || order.OrderItems.Count == 0 || order.OrderItems.Any(item => item is null))
            throw new ArgumentException("يجب إضافة عنصر واحد على الأقل للطلب، ولا يسمح بعناصر فارغة.");
    }

    private static void ValidateEnums(OrderStatus? status, OrderType? type)
    {
        if ((status.HasValue && !Enum.IsDefined(status.Value)) || (type.HasValue && !Enum.IsDefined(type.Value)))
            throw new ArgumentException("حالة الطلب أو نوعه غير صالح.");
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
        ValidateItems(orderCreateDto);
        ValidateEnums(orderCreateDto.OrderStatus, orderCreateDto.OrderType);
        Money.Validate(orderCreateDto.Discount);
        if (string.IsNullOrWhiteSpace(userId)
            || await _unitOfWork.ApplicationUsers.GetFirstOrDefaultAsync(user => user.Id == userId) == null)
        {
            throw new ArgumentException("المستخدم الذي ينشئ الطلب غير موجود.");
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
        var products = (await _unitOfWork.Products.GetAllAsync(product => productIds.Contains(product.Id), includeProperties: "Category"))
            .ToDictionary(product => product.Id);
        if (products.Count != productIds.Length)
        {
            throw new ArgumentException("أحد المنتجات المحددة غير موجود.");
        }

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            ClientRequestId = clientRequestId,
            Date = BusinessClock.Now,
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
            if (!product.IsAvailable) throw new ArgumentException("أحد الأصناف غير متوفر حالياً.");
            Money.Validate(itemDto.UnitDiscount);
            Money.Validate(itemDto.Quantity * product.CostPrice);
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
            Money.Validate(item.Total);
            OrderAccounting.Snapshot(item, product);
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
        OrderAccounting.Recalculate(order);
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
        ValidateEnums(orderUpdateDto.OrderStatus, orderUpdateDto.OrderType);
        Money.Validate(orderUpdateDto.Discount);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
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

            OrderAccounting.Recalculate(order);
            if (isCancelling)
            {
                order.CancelledAt = BusinessClock.Now;
                order.CancelledByUserId = userId;
                order.CancelledByUserName = (await _unitOfWork.ApplicationUsers.GetFirstOrDefaultAsync(u => u.Id == userId))?.UserName;
                foreach (var group in order.CashDrawerEntries
                    .Where(e => e.EntryType == CashDrawerEntryType.SalePayment && e.AffectsCash).GroupBy(e => e.CashboxId))
                {
                    var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == group.Key);
                    if (await LedgerQueries.BalanceAsync(_unitOfWork, cashbox!) < group.Sum(e => e.Amount))
                        throw new InvalidOperationException("رصيد الدرج لا يكفي للاسترداد النقدي. يجب تغذية الدرج قبل إلغاء الطلب.");
                }
                foreach (var payment in order.CashDrawerEntries
                             .Where(entry => entry.EntryType == CashDrawerEntryType.SalePayment)
                             .ToList())
                {
                    await _unitOfWork.CashDrawerEntries.AddAsync(new CashDrawerEntry
                    {
                        Date = BusinessClock.Now,
                        Amount = payment.Amount,
                        EntryType = CashDrawerEntryType.Refund,
                        Notes = $"استرداد دفعة الطلب رقم {order.OrderNumber}",
                        CashboxId = payment.CashboxId,
                        PaymentMethodId = payment.PaymentMethodId,
                        PaymentMethodNameSnapshot = payment.PaymentMethodNameSnapshot,
                        AffectsCash = payment.AffectsCash,
                        OrderId = order.Id,
                        UserId = userId
                    });
                }
            }

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveAsync();
            return true;
        });
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
