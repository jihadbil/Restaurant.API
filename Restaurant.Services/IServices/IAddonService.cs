using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IAddonService
{
    Task<IEnumerable<AddonDto>> GetAllAddonsAsync();
    Task<AddonDto?> GetAddonByIdAsync(int id);
    Task<AddonDto> CreateAddonAsync(AddonCreateDto addonCreateDto);
    Task<bool> UpdateAddonAsync(AddonUpdateDto addonUpdateDto);
    Task<bool> DeleteAddonAsync(int id);
}
