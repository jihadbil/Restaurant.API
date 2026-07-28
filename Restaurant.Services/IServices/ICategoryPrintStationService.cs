using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

/// <summary>
/// واجهة خدمة ربط وإلغاء ربط التصنيفات بمحطات الطباعة
/// </summary>
public interface ICategoryPrintStationService
{
    /// <summary>
    /// ربط تصنيف بمحطة طباعة
    /// </summary>
    Task<bool> LinkCategoryToPrintStationAsync(CategoryPrintStationCreateDto dto);

    /// <summary>
    /// إلغاء ربط تصنيف بمحطة طباعة
    /// </summary>
    Task<bool> UnlinkCategoryFromPrintStationAsync(int categoryId, int printStationId);

    /// <summary>
    /// الحصول على جميع التصنيفات المرتبطة بمحطة طباعة معينة
    /// </summary>
    Task<IEnumerable<CategoryDto>> GetCategoriesByPrintStationIdAsync(int printStationId);

    /// <summary>
    /// الحصول على جميع محطات الطباعة المرتبطة بتصنيف معين
    /// </summary>
    Task<IEnumerable<PrintStationDto>> GetPrintStationsByCategoryIdAsync(int categoryId);
}
