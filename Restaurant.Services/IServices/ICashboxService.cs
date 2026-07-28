using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface ICashboxService
{
    Task<IEnumerable<CashboxDto>> GetAllCashboxesAsync();
    Task<CashboxDto?> GetCashboxByIdAsync(int id);
    Task<CashboxBalanceDto?> GetCashboxBalanceAsync(int id);
    Task<CashboxDto> CreateCashboxAsync(CashboxCreateDto dto);
    Task<bool> UpdateCashboxAsync(CashboxUpdateDto dto);
    Task<bool> DeleteCashboxAsync(int id);
}
