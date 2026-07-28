using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IPrinterService
{
    Task<IEnumerable<PrinterDto>> GetAllPrintersAsync();
    Task<PrinterDto?> GetPrinterByIdAsync(int id);
    Task<PrinterDto> CreatePrinterAsync(PrinterCreateDto printerCreateDto);
    Task<bool> UpdatePrinterAsync(PrinterUpdateDto printerUpdateDto);
    Task<bool> DeletePrinterAsync(int id);
}
