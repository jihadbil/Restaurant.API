using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class PrinterApiService : IPrinterApiService
    {
        private readonly ApiClient _apiClient;

        public PrinterApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<PrinterDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<PrinterDto>>("api/printers");
        }

        public async Task<ApiResult<PrinterDto>> GetByIdAsync(int id)
        {
            return await _apiClient.GetAsync<PrinterDto>($"api/printers/{id}");
        }

        public async Task<ApiResult<PrinterDto>> CreateAsync(PrinterCreateDto dto)
        {
            return await _apiClient.PostAsync<PrinterCreateDto, PrinterDto>("api/printers", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, PrinterUpdateDto dto)
        {
            return await _apiClient.PutAsync<PrinterUpdateDto>($"api/printers/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/printers/{id}");
        }
    }
}
