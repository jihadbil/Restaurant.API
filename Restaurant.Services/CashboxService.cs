using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class CashboxService : ICashboxService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CashboxService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CashboxDto>> GetAllCashboxesAsync()
    {
        var cashboxes = await _unitOfWork.Cashboxes.GetAllAsync();
        return _mapper.Map<IEnumerable<CashboxDto>>(cashboxes);
    }

    public async Task<CashboxDto?> GetCashboxByIdAsync(int id)
    {
        var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == id);
        return _mapper.Map<CashboxDto?>(cashbox);
    }

    public async Task<CashboxBalanceDto?> GetCashboxBalanceAsync(int id)
    {
        var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == id);
        if (cashbox == null)
        {
            return null;
        }

        var query = _unitOfWork.CashDrawerEntries.Query().Where(e => e.CashboxId == id && e.AffectsCash);
        var totals = await query.GroupBy(e => e.CashboxId).Select(g => new
        {
            Inflow = g.Sum(e => e.EntryType == CashDrawerEntryType.SalePayment || e.EntryType == CashDrawerEntryType.Inflow
                || e.EntryType == CashDrawerEntryType.TransferIn || e.EntryType == CashDrawerEntryType.CashSurplus ? e.Amount : 0),
            Outflow = g.Sum(e => e.EntryType != CashDrawerEntryType.SalePayment && e.EntryType != CashDrawerEntryType.Inflow
                && e.EntryType != CashDrawerEntryType.TransferIn && e.EntryType != CashDrawerEntryType.CashSurplus ? e.Amount : 0)
        }).SingleOrDefaultAsync();
        var totalInflow = totals?.Inflow ?? 0;
        var totalOutflow = totals?.Outflow ?? 0;
        return new CashboxBalanceDto
        {
            Id = cashbox.Id,
            Name = cashbox.Name,
            InitialBalance = cashbox.InitialBalance,
            TotalInflow = totalInflow,
            TotalOutflow = totalOutflow,
            CurrentBalance = cashbox.InitialBalance + totalInflow - totalOutflow
        };
    }

    public async Task<CashboxDto> CreateCashboxAsync(CashboxCreateDto dto)
    {
        CashShiftService.ValidateMoney(dto.InitialBalance, allowZero: true);
        var cashbox = _mapper.Map<Cashbox>(dto);
        await _unitOfWork.Cashboxes.AddAsync(cashbox);
        await _unitOfWork.SaveAsync();
        return _mapper.Map<CashboxDto>(cashbox);
    }

    public async Task<bool> UpdateCashboxAsync(CashboxUpdateDto dto)
    {
        var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == dto.Id, tracked: false);
        if (cashbox == null)
        {
            return false;
        }

        // We map to the existing tracked object but we need to fetch it tracked if we want updates to be saved.
        // Or we retrieve tracked, map, and then SaveAsync.
        var trackedCashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == dto.Id);
        if (trackedCashbox == null)
        {
            return false;
        }

        _mapper.Map(dto, trackedCashbox);
        _unitOfWork.Cashboxes.Update(trackedCashbox);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeleteCashboxAsync(int id)
    {
        var cashbox = await _unitOfWork.Cashboxes.GetFirstOrDefaultAsync(c => c.Id == id);
        if (cashbox == null)
        {
            return false;
        }

        // Check if there are any associated cash drawer entries
        var hasEntries = (await _unitOfWork.CashDrawerEntries.GetAllAsync(e => e.CashboxId == id)).Any();
        if (hasEntries)
        {
            // Do not delete cashbox with associated entries
            return false;
        }

        if ((await _unitOfWork.CashShifts.GetAllAsync(s => s.SourceCashboxId == id || s.TargetCashboxId == id)).Any())
            return false;

        _unitOfWork.Cashboxes.Remove(cashbox);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
