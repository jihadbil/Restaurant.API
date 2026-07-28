using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface ICashDrawerEntryApiService
    {
        Task<ApiResult<CashDrawerEntryDto>> CreateAsync(CashDrawerEntryCreateDto dto);
        Task<ApiResult<List<CashDrawerEntryDto>>> GetAllAsync(int? cashboxId, DateTime? from, DateTime? to);
        Task<ApiResult<List<CashDrawerEntryDto>>> GetByOrderAsync(int orderId);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
