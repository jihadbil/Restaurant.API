using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IPrinterApiService
    {
        Task<ApiResult<List<PrinterDto>>> GetAllAsync();
        Task<ApiResult<PrinterDto>> GetByIdAsync(int id);
        Task<ApiResult<PrinterDto>> CreateAsync(PrinterCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, PrinterUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
