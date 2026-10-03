using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class CashDrawerEntryService : ICashDrawerEntryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CashDrawerEntryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CashDrawerEntryDto>> GetAllEntriesAsync(int? cashboxId, DateTime? from, DateTime? to)
    {
        var entries = await _unitOfWork.CashDrawerEntries.GetAllAsync(
            filter: e => (!cashboxId.HasValue || e.CashboxId == cashboxId.Value) &&
                         (!from.HasValue || e.Date >= from.Value) &&
                         (!to.HasValue || e.Date <= to.Value),
            includeProperties: "Cashbox,PaymentMethod,Order,User"
        );
        return _mapper.Map<IEnumerable<CashDrawerEntryDto>>(entries);
    }

    public async Task<CashDrawerEntryDto?> GetEntryByIdAsync(int id)
    {
        var entry = await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(
            e => e.Id == id,
            includeProperties: "Cashbox,PaymentMethod,Order,User"
        );
        return _mapper.Map<CashDrawerEntryDto?>(entry);
    }

    public async Task<IEnumerable<CashDrawerEntryDto>> GetEntriesByOrderAsync(int orderId)
    {
        var entries = await _unitOfWork.CashDrawerEntries.GetAllAsync(
            filter: e => e.OrderId == orderId,
            includeProperties: "Cashbox,PaymentMethod,Order,User"
        );
        return _mapper.Map<IEnumerable<CashDrawerEntryDto>>(entries);
    }

    public async Task<CashDrawerEntryDto> CreateEntryAsync(CashDrawerEntryCreateDto dto, string userId)
    {
        if (dto.EntryType == CashDrawerEntryType.SalePayment || dto.EntryType == CashDrawerEntryType.Refund)
        {
            throw new ArgumentException("حركات البيع والاسترداد تُنشأ فقط من خلال عمليات الطلبات.");
        }

        if (dto.EntryType != CashDrawerEntryType.Inflow && dto.EntryType != CashDrawerEntryType.Outflow)
        {
            throw new ArgumentException("نوع حركة الخزينة غير صالح.");
        }

        if (string.IsNullOrWhiteSpace(userId)
            || await _unitOfWork.ApplicationUsers.GetFirstOrDefaultAsync(user => user.Id == userId) == null)
        {
            throw new ArgumentException("المستخدم غير موجود.");
        }

        // 1. Verify Cashbox
        var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == dto.CashboxId);
        if (cashbox == null)
        {
            throw new ArgumentException("الخزينة المحددة غير موجودة.");
        }
        if (!cashbox.IsActive)
        {
            throw new ArgumentException("الخزينة المحددة غير نشطة.");
        }

        if (dto.OrderId.HasValue || dto.PaymentMethodId.HasValue)
        {
            throw new ArgumentException("الإيداع والسحب اليدوي لا يرتبطان بطلب أو بطريقة دفع.");
        }

        var entry = _mapper.Map<CashDrawerEntry>(dto);
        entry.Date = DateTime.Now;
        entry.UserId = userId;

        await _unitOfWork.CashDrawerEntries.AddAsync(entry);
        await _unitOfWork.SaveAsync();

        // Fetch saved entry with include properties to populate DTO names correctly
        var savedEntry = await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(
            e => e.Id == entry.Id,
            includeProperties: "Cashbox,PaymentMethod,Order,User"
        );

        return _mapper.Map<CashDrawerEntryDto>(savedEntry!);
    }

    public async Task<bool> DeleteEntryAsync(int id)
    {
        var entry = await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(e => e.Id == id);
        if (entry == null)
        {
            return false;
        }

        if (entry.EntryType == CashDrawerEntryType.SalePayment || entry.EntryType == CashDrawerEntryType.Refund)
        {
            throw new InvalidOperationException("لا يمكن حذف حركة بيع أو استرداد مرتبطة بطلب.");
        }

        _unitOfWork.CashDrawerEntries.Remove(entry);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
