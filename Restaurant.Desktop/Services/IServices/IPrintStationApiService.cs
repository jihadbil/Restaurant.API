using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IPrintStationApiService
    {
        Task<ApiResult<List<PrintStationDto>>> GetAllAsync();
        Task<ApiResult<PrintStationDto>> GetByIdAsync(int id);
        Task<ApiResult<PrintStationDto>> CreateAsync(PrintStationCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, PrintStationUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
        Task<ApiResult<List<PrintStationDto>>> GetStationsByCategoryIdAsync(int categoryId);
        Task<ApiResult<bool>> LinkCategoryToStationAsync(int categoryId, int stationId);
        Task<ApiResult<bool>> UnlinkCategoryFromStationAsync(int categoryId, int stationId);
    }
}
