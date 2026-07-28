using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IPrintStationService
{
    Task<IEnumerable<PrintStationDto>> GetAllPrintStationsAsync();
    Task<PrintStationDto?> GetPrintStationByIdAsync(int id);
    Task<PrintStationDto> CreatePrintStationAsync(PrintStationCreateDto printStationCreateDto);
    Task<bool> UpdatePrintStationAsync(PrintStationUpdateDto printStationUpdateDto);
    Task<bool> DeletePrintStationAsync(int id);
}
