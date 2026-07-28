using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class AddonService : IAddonService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddonService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AddonDto>> GetAllAddonsAsync()
    {
        var addons = await _unitOfWork.Addons.GetAllAsync();
        return _mapper.Map<IEnumerable<AddonDto>>(addons);
    }

    public async Task<AddonDto?> GetAddonByIdAsync(int id)
    {
        var addon = await _unitOfWork.Addons.GetFirstOrDefaultAsync(a => a.Id == id);
        return _mapper.Map<AddonDto?>(addon);
    }

    public async Task<AddonDto> CreateAddonAsync(AddonCreateDto addonCreateDto)
    {
        var addon = _mapper.Map<Addon>(addonCreateDto);
        await _unitOfWork.Addons.AddAsync(addon);
        await _unitOfWork.SaveAsync();
        return _mapper.Map<AddonDto>(addon);
    }

    public async Task<bool> UpdateAddonAsync(AddonUpdateDto addonUpdateDto)
    {
        var addon = await _unitOfWork.Addons.GetFirstOrDefaultAsync(a => a.Id == addonUpdateDto.Id, tracked: false);
        if (addon == null)
        {
            return false;
        }

        _mapper.Map(addonUpdateDto, addon);
        _unitOfWork.Addons.Update(addon);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeleteAddonAsync(int id)
    {
        var addon = await _unitOfWork.Addons.GetFirstOrDefaultAsync(a => a.Id == id);
        if (addon == null)
        {
            return false;
        }

        _unitOfWork.Addons.Remove(addon);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
