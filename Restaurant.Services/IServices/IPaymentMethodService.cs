using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IPaymentMethodService
{
    Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync();
    Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(int id);
    Task<PaymentMethodDto> CreatePaymentMethodAsync(PaymentMethodCreateDto paymentMethodCreateDto);
    Task<bool> UpdatePaymentMethodAsync(PaymentMethodUpdateDto paymentMethodUpdateDto);
    Task<bool> DeletePaymentMethodAsync(int id);
}
