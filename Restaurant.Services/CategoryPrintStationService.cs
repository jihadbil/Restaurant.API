using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Services;

/// <summary>
/// تنفيذ خدمة ربط وإلغاء ربط التصنيفات بمحطات الطباعة في المطعم
/// </summary>
public class CategoryPrintStationService : ICategoryPrintStationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryPrintStationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<bool> LinkCategoryToPrintStationAsync(CategoryPrintStationCreateDto dto)
    {
        // التحقق من وجود التصنيف ومحطة الطباعة
        var category = await _unitOfWork.Categories.GetFirstOrDefaultAsync(c => c.Id == dto.CategoryId);
        var printStation = await _unitOfWork.PrintStations.GetFirstOrDefaultAsync(ps => ps.Id == dto.PrintStationId);

        if (category == null || printStation == null)
        {
            return false;
        }

        // التحقق من عدم التكرار
        var existingLink = await _unitOfWork.CategoryPrintStations.GetFirstOrDefaultAsync(
            cps => cps.CategoryId == dto.CategoryId && cps.PrintStationId == dto.PrintStationId
        );

        if (existingLink != null)
        {
            return true; // مرتبط بالفعل
        }

        var link = _mapper.Map<CategoryPrintStation>(dto);
        await _unitOfWork.CategoryPrintStations.AddAsync(link);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> UnlinkCategoryFromPrintStationAsync(int categoryId, int printStationId)
    {
        var existingLink = await _unitOfWork.CategoryPrintStations.GetFirstOrDefaultAsync(
            cps => cps.CategoryId == categoryId && cps.PrintStationId == printStationId
        );

        if (existingLink == null)
        {
            return false;
        }

        _unitOfWork.CategoryPrintStations.Remove(existingLink);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesByPrintStationIdAsync(int printStationId)
    {
        var links = await _unitOfWork.CategoryPrintStations.GetAllAsync(
            cps => cps.PrintStationId == printStationId,
            includeProperties: "Category"
        );

        var categories = links.Select(l => l.Category).Where(c => c != null);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<IEnumerable<PrintStationDto>> GetPrintStationsByCategoryIdAsync(int categoryId)
    {
        var links = await _unitOfWork.CategoryPrintStations.GetAllAsync(
            cps => cps.CategoryId == categoryId,
            includeProperties: "PrintStation"
        );

        var stations = links.Select(l => l.PrintStation).Where(s => s != null);
        return _mapper.Map<IEnumerable<PrintStationDto>>(stations);
    }
}
