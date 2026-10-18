using Restaurant.DataAccess.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;

namespace Restaurant.Services;

public class CashShiftService(IUnitOfWork unitOfWork) : ICashShiftService
{
    public async Task<CashShiftContextDto> GetContextAsync(int cashboxId, bool canViewTotals)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var source = await GetSourceAsync(cashboxId);
            var latest = await LatestAsync(cashboxId);
            var entries = (await unitOfWork.CashDrawerEntries.GetAllAsync(e => e.CashboxId == cashboxId && e.Id > (latest == null ? 0 : latest.LastEntryId), includeProperties: "PaymentMethod")).ToList();
            var period = entries.Where(e => e.Id > (latest?.LastEntryId ?? 0)).ToList();
            return new CashShiftContextDto
            {
                SourceCashboxId = source.Id,
                SourceCashboxName = source.Name,
                PreviousShiftId = latest?.Id ?? 0,
                LastEntryId = entries.Select(e => e.Id).DefaultIfEmpty(latest?.LastEntryId ?? 0).Max(),
                StartedAt = latest?.ClosedAt ?? entries.Select(e => e.Date).DefaultIfEmpty(BusinessClock.Now).Min(),
                CanClose = latest == null || period.Count > 0,
                ExpectedCash = canViewTotals ? await LedgerQueries.BalanceAsync(unitOfWork, source) : null,
                TotalSales = canViewTotals ? period.Where(e => e.EntryType == CashDrawerEntryType.SalePayment).Sum(e => e.Amount) : null,
                TotalRefunds = canViewTotals ? period.Where(e => e.EntryType == CashDrawerEntryType.Refund).Sum(e => e.Amount) : null,
                TotalManualInflow = canViewTotals ? period.Where(e => e.AffectsCash && CashLedger.IsInflow(e.EntryType) && e.EntryType != CashDrawerEntryType.SalePayment).Sum(e => e.Amount) : null,
                TotalOutflow = canViewTotals ? period.Where(e => e.AffectsCash && !CashLedger.IsInflow(e.EntryType)).Sum(e => e.Amount) : null,
                TotalOrdersCount = canViewTotals ? period.Where(e => e.EntryType == CashDrawerEntryType.SalePayment && e.OrderId.HasValue).Select(e => e.OrderId).Distinct().Count() : null,
                PaymentSummary = canViewTotals ? period.Where(e => e.EntryType is CashDrawerEntryType.SalePayment or CashDrawerEntryType.Refund)
                    .GroupBy(e => e.PaymentMethodNameSnapshot ?? e.PaymentMethod?.Name ?? (e.AffectsCash ? "نقد" : "دفع إلكتروني"))
                    .Select(g => new CashShiftPaymentSummaryDto { PaymentMethodName = g.Key, Total = g.Sum(e => e.EntryType == CashDrawerEntryType.Refund ? -e.Amount : e.Amount) }).ToList() : null
            };
        });
    }

    public Task<CashShiftDto> CloseAsync(CashShiftCloseDto dto, string userId, bool canViewTotals) =>
        unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var actualCash = dto.ActualCash ?? throw new ArgumentException("المبلغ المعدود مطلوب.");
            ValidateMoney(actualCash, allowZero: true);
            if (string.IsNullOrWhiteSpace(dto.ClientRequestId) || dto.ClientRequestId.Length > 100)
                throw new ArgumentException("معرف عملية إغلاق الوردية مطلوب.");
            if (dto.Notes?.Length > 500) throw new ArgumentException("الملاحظات لا تتجاوز 500 حرف.");
            var existing = await unitOfWork.CashShifts.GetFirstOrDefaultAsync(
                s => s.UserId == userId && s.ClientRequestId == dto.ClientRequestId,
                includeProperties: "SourceCashbox,TargetCashbox,User");
            if (existing != null)
            {
                if (existing.SourceCashboxId != dto.SourceCashboxId || existing.TargetCashboxId != dto.TargetCashboxId
                    || existing.ActualCash != actualCash)
                    throw new InvalidOperationException("معرف الإغلاق مستخدم لعملية مختلفة.");
                return ToDto(existing, canViewTotals);
            }
            var user = await unitOfWork.ApplicationUsers.GetFirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new ArgumentException("المستخدم غير موجود.");
            var source = await GetSourceAsync(dto.SourceCashboxId);
            var target = await unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == dto.TargetCashboxId);
            if (target == null || !target.IsActive || target.Id == source.Id || target.IsDrawer)
                throw new ArgumentException("اختر خزينة مستلمة نشطة تختلف عن درج نقطة البيع.");
            var latest = await LatestAsync(source.Id);
            var entries = (await unitOfWork.CashDrawerEntries.GetAllAsync(e => e.CashboxId == source.Id && e.Id > (latest == null ? 0 : latest.LastEntryId))).ToList();
            var lastEntryId = entries.Select(e => e.Id).DefaultIfEmpty(latest?.LastEntryId ?? 0).Max();
            if ((latest?.Id ?? 0) != dto.PreviousShiftId || lastEntryId != dto.LastEntryId)
                throw new InvalidOperationException("تغيرت حركات الدرج أو أُغلقت الوردية. حدّث البيانات قبل التأكيد.");
            if (latest != null && lastEntryId <= latest.LastEntryId)
                throw new InvalidOperationException("الوردية مغلقة بالفعل ولا توجد حركات جديدة.");

            var expected = await LedgerQueries.BalanceAsync(unitOfWork, source);
            var now = BusinessClock.Now;
            var shift = new CashShift
            {
                SourceCashboxId = source.Id,
                TargetCashboxId = target.Id,
                SourceCashbox = source,
                TargetCashbox = target,
                User = user,
                UserId = userId,
                PreviousShiftId = latest?.Id ?? 0,
                StartedAt = latest?.ClosedAt ?? entries.Select(e => e.Date).DefaultIfEmpty(now).Min(),
                ClosedAt = now,
                ExpectedCash = expected,
                ActualCash = actualCash,
                Difference = actualCash - expected,
                Notes = dto.Notes?.Trim(),
                ClientRequestId = dto.ClientRequestId
            };
            await unitOfWork.CashShifts.AddAsync(shift);
            if (shift.Difference != 0)
                await AddEntryAsync(source.Id, Math.Abs(shift.Difference),
                    shift.Difference > 0 ? CashDrawerEntryType.CashSurplus : CashDrawerEntryType.CashShortage,
                    "مطابقة نقدية إغلاق الوردية", shift);
            if (actualCash > 0)
            {
                await AddEntryAsync(source.Id, actualCash, CashDrawerEntryType.TransferOut,
                    $"توريد نقدية الوردية إلى {target.Name}", shift);
                await AddEntryAsync(target.Id, actualCash, CashDrawerEntryType.TransferIn,
                    $"استلام نقدية الوردية من {source.Name}", shift);
            }
            // One transaction contains the closure, reconciliation and both transfer legs.
            await unitOfWork.SaveAsync();
            shift.LastEntryId = shift.Entries.Where(e => e.CashboxId == source.Id)
                .Select(e => e.Id).Append(lastEntryId).Max();
            await unitOfWork.SaveAsync();
            return ToDto(shift, canViewTotals);
        });

    public async Task<IEnumerable<CashShiftDto>> GetHistoryAsync(int? cashboxId) =>
        (await unitOfWork.CashShifts.GetAllAsync(
            s => !cashboxId.HasValue || s.SourceCashboxId == cashboxId,
            orderBy: q => q.OrderByDescending(s => s.Id), includeProperties: "SourceCashbox,TargetCashbox,User"))
        .Select(s => ToDto(s, true));

    public async Task<CashShiftDto?> GetByRequestAsync(string requestId, string userId, bool canViewTotals)
    {
        var saved = await unitOfWork.CashShifts.GetFirstOrDefaultAsync(
            s => s.UserId == userId && s.ClientRequestId == requestId,
            includeProperties: "SourceCashbox,TargetCashbox,User");
        return saved == null ? null : ToDto(saved, canViewTotals);
    }

    private async Task<Cashbox> GetSourceAsync(int id)
    {
        var source = await unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == id);
        if (source == null || !source.IsActive || !source.IsDrawer)
            throw new ArgumentException("خزينة الدرج غير موجودة أو غير نشطة. اضبط نوع الخزينة من الإعدادات.");
        return source;
    }

    private Task<CashShift?> LatestAsync(int id) =>
        unitOfWork.CashShifts.Query().Where(s => s.SourceCashboxId == id).OrderByDescending(s => s.Id).FirstOrDefaultAsync();

    private Task AddEntryAsync(int cashboxId, decimal amount, CashDrawerEntryType type, string notes, CashShift shift) =>
        unitOfWork.CashDrawerEntries.AddAsync(new CashDrawerEntry
        {
            CashboxId = cashboxId,
            Amount = amount,
            EntryType = type,
            AffectsCash = true,
            Date = shift.ClosedAt,
            UserId = shift.UserId,
            Notes = notes,
            CashShift = shift
        });

    internal static void ValidateMoney(decimal amount, bool allowZero = false) => Money.Validate(amount, allowZero);

    private static CashShiftDto ToDto(CashShift s, bool totals) => new()
    {
        Id = s.Id,
        SourceCashboxId = s.SourceCashboxId,
        SourceCashboxName = s.SourceCashbox.Name,
        TargetCashboxId = s.TargetCashboxId,
        TargetCashboxName = s.TargetCashbox.Name,
        StartedAt = s.StartedAt,
        ClosedAt = s.ClosedAt,
        ActualCash = s.ActualCash,
        ExpectedCash = totals ? s.ExpectedCash : null,
        Difference = totals ? s.Difference : null,
        UserId = s.UserId,
        UserName = s.User.UserName ?? string.Empty,
        Notes = s.Notes
    };
}
