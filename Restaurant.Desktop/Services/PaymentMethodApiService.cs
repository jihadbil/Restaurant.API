using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class PaymentMethodApiService : IPaymentMethodApiService
    {
        private readonly ApiClient _apiClient;

        public PaymentMethodApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<List<PaymentMethodDto>>> GetAllAsync()
        {
            return await _apiClient.GetAsync<List<PaymentMethodDto>>("api/paymentmethods");
        }

        public async Task<ApiResult<PaymentMethodDto>> CreateAsync(PaymentMethodCreateDto dto)
        {
            return await _apiClient.PostAsync<PaymentMethodCreateDto, PaymentMethodDto>("api/paymentmethods", dto);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, PaymentMethodUpdateDto dto)
        {
            return await _apiClient.PutAsync<PaymentMethodUpdateDto>($"api/paymentmethods/{id}", dto);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            return await _apiClient.DeleteAsync($"api/paymentmethods/{id}");
        }

        public async Task<ApiResult<string>> UploadLogoAsync(string filePath)
        {
            var result = await _apiClient.PostFileAsync<ImageUploadResponse>("api/paymentmethods/upload-logo", filePath);
            if (result.IsSuccess && result.Data != null)
            {
                return ApiResult<string>.Success(result.Data.ImageUrl, result.StatusCode);
            }
            return ApiResult<string>.Failure(result.ErrorMessage ?? "فشل رفع الشعار", result.StatusCode);
        }
    }
}
