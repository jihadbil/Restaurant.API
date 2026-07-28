using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetFirstOrDefaultAsync(c => c.Id == id);
        return _mapper.Map<CategoryDto?>(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto)
    {
        var category = _mapper.Map<Category>(categoryCreateDto);
        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveAsync();
        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> UpdateCategoryAsync(CategoryUpdateDto categoryUpdateDto)
    {
        var category = await _unitOfWork.Categories.GetFirstOrDefaultAsync(c => c.Id == categoryUpdateDto.Id, tracked: false);
        if (category == null)
        {
            return false;
        }

        _mapper.Map(categoryUpdateDto, category);
        _unitOfWork.Categories.Update(category);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetFirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return false;
        }

        _unitOfWork.Categories.Remove(category);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
