using Restaurant.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface ICashDrawerEntryService
{
    Task<IEnumerable<CashDrawerEntryDto>> GetAllEntriesAsync(int? cashboxId, DateTime? from, DateTime? to);
    Task<CashDrawerEntryDto?> GetEntryByIdAsync(int id);
    Task<IEnumerable<CashDrawerEntryDto>> GetEntriesByOrderAsync(int orderId);
    Task<CashDrawerEntryDto> CreateEntryAsync(CashDrawerEntryCreateDto dto);
    Task<bool> DeleteEntryAsync(int id);
}
