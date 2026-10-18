using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        if (from > to) throw new ArgumentException("تاريخ البداية يجب أن يسبق تاريخ النهاية.");
        if (to.HasValue && to.Value.Date == DateTime.MaxValue.Date) throw new ArgumentException("تاريخ النهاية خارج النطاق المسموح.");
        var exclusiveTo = to.HasValue && to.Value.TimeOfDay == TimeSpan.Zero
            ? to.Value.AddDays(1) : (DateTime?)null;
        var entries = await _unitOfWork.CashDrawerEntries.GetAllAsync(
            filter: e => (!cashboxId.HasValue || e.CashboxId == cashboxId.Value) &&
                         (!from.HasValue || e.Date >= from.Value) &&
                         (!to.HasValue || (exclusiveTo.HasValue ? e.Date < exclusiveTo.Value : e.Date <= to.Value)),
            orderBy: q => q.OrderByDescending(e => e.Id),
            includeProperties: "Cashbox,PaymentMethod,Order,User"
        );
        return _mapper.Map<IEnumerable<CashDrawerEntryDto>>(entries);
    }

    public async Task<CashEntryPageDto> GetEntriesPageAsync(CashEntryQueryDto filter)
    {
        if (filter.Page < 1 || filter.Page > 100000 || filter.PageSize < 1 || filter.PageSize > 200)
            throw new ArgumentException("حدود الصفحة غير صالحة.");
        var period = ReportPeriod.Create(filter.From ?? BusinessClock.Today.AddDays(-30), filter.To ?? BusinessClock.Today);
        var query = _unitOfWork.CashDrawerEntries.Query().Where(e => e.Date >= period.Start && e.Date < period.EndExclusive);
        if (filter.CashboxId.HasValue) query = query.Where(e => e.CashboxId == filter.CashboxId);
        if (filter.EntryType.HasValue) query = query.Where(e => e.EntryType == filter.EntryType);
        var summary = await query.GroupBy(e => 1).Select(g => new
        {
            Count = g.Count(),
            Inflow = g.Sum(e => e.AffectsCash && (e.EntryType == CashDrawerEntryType.Inflow || e.EntryType == CashDrawerEntryType.TransferIn
                || e.EntryType == CashDrawerEntryType.CashSurplus) ? e.Amount : 0),
            Outflow = g.Sum(e => e.AffectsCash && (e.EntryType == CashDrawerEntryType.Outflow || e.EntryType == CashDrawerEntryType.TransferOut
                || e.EntryType == CashDrawerEntryType.CashShortage || e.EntryType == CashDrawerEntryType.Refund) ? e.Amount : 0),
            Sales = g.Sum(e => e.EntryType == CashDrawerEntryType.SalePayment ? e.Amount : 0)
        }).SingleOrDefaultAsync();
        var entries = await query.OrderByDescending(e => e.Id).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Include(e => e.Cashbox).Include(e => e.PaymentMethod).Include(e => e.Order).Include(e => e.User).ToListAsync();
        return new(_mapper.Map<List<CashDrawerEntryDto>>(entries), summary?.Count ?? 0, filter.Page, filter.PageSize,
            summary?.Inflow ?? 0, summary?.Outflow ?? 0, summary?.Sales ?? 0);
    }

    public async Task<CashDrawerEntryDto?> GetByRequestAsync(string requestId, string userId) =>
        _mapper.Map<CashDrawerEntryDto?>(await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(
            e => e.ClientRequestId == requestId && e.UserId == userId, includeProperties: "Cashbox,PaymentMethod,Order,User", tracked: false));

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
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (dto.EntryType == CashDrawerEntryType.SalePayment || dto.EntryType == CashDrawerEntryType.Refund)
            {
                throw new ArgumentException("حركات البيع والاسترداد تُنشأ فقط من خلال عمليات الطلبات.");
            }

            if (dto.EntryType != CashDrawerEntryType.Inflow && dto.EntryType != CashDrawerEntryType.Outflow)
            {
                throw new ArgumentException("نوع حركة الخزينة غير صالح.");
            }

            CashShiftService.ValidateMoney(dto.Amount);
            if (dto.Notes?.Length > 500 || dto.ClientRequestId?.Length > 100)
                throw new ArgumentException("الملاحظات أو معرف العملية أطول من الحد المسموح.");
            if (!string.IsNullOrWhiteSpace(dto.ClientRequestId))
            {
                var duplicate = await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(
                    e => e.UserId == userId && e.ClientRequestId == dto.ClientRequestId,
                    includeProperties: "Cashbox,PaymentMethod,Order,User");
                if (duplicate != null)
                {
                    if (duplicate.CashboxId != dto.CashboxId || duplicate.Amount != dto.Amount || duplicate.EntryType != dto.EntryType)
                        throw new InvalidOperationException("معرف العملية مستخدم لحركة مختلفة.");
                    return _mapper.Map<CashDrawerEntryDto>(duplicate);
                }
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

            if (dto.EntryType == CashDrawerEntryType.Outflow)
            {
                if (dto.Amount > await LedgerQueries.BalanceAsync(_unitOfWork, cashbox))
                    throw new InvalidOperationException("رصيد الخزينة لا يكفي للسحب.");
            }

            var entry = _mapper.Map<CashDrawerEntry>(dto);
            entry.Date = BusinessClock.Now;
            entry.UserId = userId;
            entry.AffectsCash = true;
            entry.ClientRequestId = string.IsNullOrWhiteSpace(dto.ClientRequestId) ? null : dto.ClientRequestId;

            await _unitOfWork.CashDrawerEntries.AddAsync(entry);
            await _unitOfWork.SaveAsync();

            // Fetch saved entry with include properties to populate DTO names correctly
            var savedEntry = await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(
                e => e.Id == entry.Id,
                includeProperties: "Cashbox,PaymentMethod,Order,User"
            );

            return _mapper.Map<CashDrawerEntryDto>(savedEntry!);
        });
    }

    public async Task<bool> DeleteEntryAsync(int id)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entry = await _unitOfWork.CashDrawerEntries.GetFirstOrDefaultAsync(e => e.Id == id);
            if (entry == null)
            {
                return false;
            }

            if (entry.EntryType != CashDrawerEntryType.Inflow && entry.EntryType != CashDrawerEntryType.Outflow)
            {
                throw new InvalidOperationException("لا يمكن حذف حركة بيع أو استرداد أو مطابقة أو تحويل وردية.");
            }

            if ((await _unitOfWork.CashShifts.GetAllAsync(s => s.SourceCashboxId == entry.CashboxId && s.LastEntryId >= entry.Id)).Any())
                throw new InvalidOperationException("لا يمكن حذف حركة ضمن وردية مغلقة.");

            if (entry.EntryType == CashDrawerEntryType.Inflow)
            {
                var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == entry.CashboxId);
                var entries = await _unitOfWork.CashDrawerEntries.GetAllAsync(e => e.CashboxId == entry.CashboxId);
                if (CashLedger.Balance(cashbox!.InitialBalance, entries.Where(e => e.Id != id)) < 0)
                    throw new InvalidOperationException("حذف الإيداع سيجعل رصيد الخزينة سالباً.");
            }

            _unitOfWork.CashDrawerEntries.Remove(entry);
            var result = await _unitOfWork.SaveAsync();
            return result > 0;
        });
    }
}
