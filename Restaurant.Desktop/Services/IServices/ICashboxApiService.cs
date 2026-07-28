using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface ICashboxApiService
    {
        Task<ApiResult<List<CashboxDto>>> GetAllAsync();
        Task<ApiResult<CashboxBalanceDto>> GetBalanceAsync(int id);
        Task<ApiResult<CashboxDto>> CreateAsync(CashboxCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, CashboxUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
