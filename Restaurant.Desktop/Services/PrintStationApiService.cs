using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class PrintStationApiService : IPrintStationApiService
    {
        private readonly ApiClient _apiClient;

        public PrintStationApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<PrintStationDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<PrintStationDto>>("api/printstations");
        }

        public async Task<ApiResult<PrintStationDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<PrintStationDto>($"api/printstations/{id}");
        }

        public async Task<ApiResult<PrintStationDto>> CreateAsync(PrintStationCreateDto dto)
        {
            return await _apiClient.PostAsync<PrintStationCreateDto, PrintStationDto>("api/printstations", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, PrintStationUpdateDto dto)
        {
            return await _apiClient.PutAsync<PrintStationUpdateDto>($"api/printstations/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/printstations/{id}");
        }

        public async Task<ApiResult<List<PrintStationDto>>> GetStationsByCategoryIdAsync(int categoryId)
        {
            return await _apiClient.GetAsync<List<PrintStationDto>>($"api/CategoryPrintStations/category/{categoryId}");
        }

        public async Task<ApiResult<bool>> LinkCategoryToStationAsync(int categoryId, int stationId)
        {
            var dto = new CategoryPrintStationCreateDto { CategoryId = categoryId, PrintStationId = stationId };
            var result = await _apiClient.PostAsync<CategoryPrintStationCreateDto, object>("api/CategoryPrintStations/link", dto);
            if (result.IsSuccess)
            {
                return ApiResult<bool>.Success(true, result.StatusCode);
            }
            return ApiResult<bool>.Failure(result.ErrorMessage ?? "فشل ربط التصنيف بالمحطة.", result.StatusCode);
        }

        public async Task<ApiResult<bool>> UnlinkCategoryFromStationAsync(int categoryId, int stationId)
        {
            return await _apiClient.DeleteAsync($"api/CategoryPrintStations/unlink/{categoryId}/{stationId}");
        }
    }
}
