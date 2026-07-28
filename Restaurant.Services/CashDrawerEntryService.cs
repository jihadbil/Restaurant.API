using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
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

    public async Task<CashDrawerEntryDto> CreateEntryAsync(CashDrawerEntryCreateDto dto)
    {
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

        // 2. Verify Order if provided
        if (dto.OrderId.HasValue)
        {
            var order = await _unitOfWork.Orders.GetFirstOrDefaultAsync(o => o.Id == dto.OrderId.Value);
            if (order == null)
            {
                throw new ArgumentException("الطلب المحدد غير موجود.");
            }
        }

        // 3. Verify PaymentMethod if provided
        if (dto.PaymentMethodId.HasValue)
        {
            var pm = await _unitOfWork.PaymentMethods.GetFirstOrDefaultAsync(p => p.Id == dto.PaymentMethodId.Value);
            if (pm == null)
            {
                throw new ArgumentException("طريقة الدفع المحددة غير موجودة.");
            }
        }

        var entry = _mapper.Map<CashDrawerEntry>(dto);
        entry.Date = DateTime.Now;

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

        _unitOfWork.CashDrawerEntries.Remove(entry);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
