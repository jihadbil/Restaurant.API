using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

/// <summary>
/// وحدة تحكم لإدارة العلاقة والربط بين التصنيفات ومحطات الطباعة
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoryPrintStationsController : ControllerBase
{
    private readonly ICategoryPrintStationService _service;

    public CategoryPrintStationsController(ICategoryPrintStationService service)
    {
        _service = service;
    }

    /// <summary>
    /// ربط تصنيف بمحطة طباعة معينة
    /// </summary>
    /// <param name="dto">نموذج بيانات الربط</param>
    /// <returns>نتيجة عملية الربط</returns>
    [HttpPost("link")]
    public async Task<IActionResult> LinkCategoryToPrintStation([FromBody] CategoryPrintStationCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _service.LinkCategoryToPrintStationAsync(dto);
        if (!success)
        {
            return BadRequest(new { message = "فشلت عملية الربط. يرجى التحقق من صحة معرف التصنيف ومعرف محطة الطباعة." });
        }

        return Ok(new { message = "تم ربط التصنيف بمحطة الطباعة بنجاح." });
    }

    /// <summary>
    /// إلغاء ربط تصنيف بمحطة طباعة معينة
    /// </summary>
    /// <param name="categoryId">معرف التصنيف</param>
    /// <param name="printStationId">معرف محطة الطباعة</param>
    /// <returns>نتيجة عملية إلغاء الربط</returns>
    [HttpDelete("unlink/{categoryId}/{printStationId}")]
    public async Task<IActionResult> UnlinkCategoryFromPrintStation(int categoryId, int printStationId)
    {
        var success = await _service.UnlinkCategoryFromPrintStationAsync(categoryId, printStationId);
        if (!success)
        {
            return NotFound(new { message = "لم يتم العثور على ارتباط بين هذا التصنيف ومحطة الطباعة هذه." });
        }

        return Ok(new { message = "تم إلغاء ربط التصنيف بمحطة الطباعة بنجاح." });
    }

    /// <summary>
    /// الحصول على جميع التصنيفات المرتبطة بمحطة طباعة معينة
    /// </summary>
    /// <param name="stationId">معرف محطة الطباعة</param>
    /// <returns>قائمة بالتصنيفات المرتبطة بالمحطة</returns>
    [HttpGet("station/{stationId}")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategoriesByPrintStation(int stationId)
    {
        var categories = await _service.GetCategoriesByPrintStationIdAsync(stationId);
        return Ok(categories);
    }

    /// <summary>
    /// الحصول على جميع محطات الطباعة المرتبطة بتصنيف معين
    /// </summary>
    /// <param name="categoryId">معرف التصنيف</param>
    /// <returns>قائمة بمحطات الطباعة المرتبطة بالتصنيف</returns>
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<PrintStationDto>>> GetPrintStationsByCategory(int categoryId)
    {
        var stations = await _service.GetPrintStationsByCategoryIdAsync(categoryId);
        return Ok(stations);
    }
}
