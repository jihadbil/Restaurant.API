using Restaurant.Models.DTOs;

namespace Restaurant.Services.IServices;

public interface ICashShiftService
{
    Task<CashShiftContextDto> GetContextAsync(int cashboxId, bool canViewTotals);
    Task<CashShiftDto> CloseAsync(CashShiftCloseDto dto, string userId, bool canViewTotals);
    Task<IEnumerable<CashShiftDto>> GetHistoryAsync(int? cashboxId);
    Task<CashShiftDto?> GetByRequestAsync(string requestId, string userId, bool canViewTotals);
}
