using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PaymentMethodService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync()
    {
        var methods = await _unitOfWork.PaymentMethods.GetAllAsync();
        return _mapper.Map<IEnumerable<PaymentMethodDto>>(methods);
    }

    public async Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(int id)
    {
        var method = await _unitOfWork.PaymentMethods.GetFirstOrDefaultAsync(pm => pm.Id == id);
        return _mapper.Map<PaymentMethodDto?>(method);
    }

    public async Task<PaymentMethodDto> CreatePaymentMethodAsync(PaymentMethodCreateDto paymentMethodCreateDto)
    {
        var method = _mapper.Map<PaymentMethod>(paymentMethodCreateDto);
        await _unitOfWork.PaymentMethods.AddAsync(method);
        await _unitOfWork.SaveAsync();
        return _mapper.Map<PaymentMethodDto>(method);
    }

    public async Task<bool> UpdatePaymentMethodAsync(PaymentMethodUpdateDto paymentMethodUpdateDto)
    {
        var method = await _unitOfWork.PaymentMethods.GetFirstOrDefaultAsync(pm => pm.Id == paymentMethodUpdateDto.Id, tracked: false);
        if (method == null)
        {
            return false;
        }

        _mapper.Map(paymentMethodUpdateDto, method);
        _unitOfWork.PaymentMethods.Update(method);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeletePaymentMethodAsync(int id)
    {
        var method = await _unitOfWork.PaymentMethods.GetFirstOrDefaultAsync(pm => pm.Id == id);
        if (method == null)
        {
            return false;
        }

        _unitOfWork.PaymentMethods.Remove(method);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
