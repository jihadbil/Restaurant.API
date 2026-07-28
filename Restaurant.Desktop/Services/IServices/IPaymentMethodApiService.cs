using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IPaymentMethodApiService
    {
        Task<ApiResult<List<PaymentMethodDto>>> GetAllAsync();
        Task<ApiResult<PaymentMethodDto>> CreateAsync(PaymentMethodCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, PaymentMethodUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
        Task<ApiResult<string>> UploadLogoAsync(string filePath);
    }
}
