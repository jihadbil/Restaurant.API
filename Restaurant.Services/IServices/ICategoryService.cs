using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto);
    Task<bool> UpdateCategoryAsync(CategoryUpdateDto categoryUpdateDto);
    Task<bool> DeleteCategoryAsync(int id);
}
