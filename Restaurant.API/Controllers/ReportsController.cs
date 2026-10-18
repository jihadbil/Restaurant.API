using Restaurant.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

/// <summary>
/// متحكم التقارير والإحصائيات الخاصة بالبائع
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("dashboard"), Authorize(Policy = "Permission.Dashboard")]
    public async Task<ActionResult<ComprehensiveReportDto>> Dashboard([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate) =>
        Ok(await _reportService.GetComprehensiveReportAsync(startDate ?? BusinessClock.Today.AddDays(-30), endDate ?? BusinessClock.Today));

    /// <summary>
    /// الحصول على التقرير الشامل والمؤشرات الأساسية (لوحة القيادة)
    /// </summary>
    /// <param name="startDate">تاريخ البدء (افتراضياً قبل 30 يوماً)</param>
    /// <param name="endDate">تاريخ النهاية (افتراضياً اليوم)</param>
    [HttpGet("comprehensive")]
    [Authorize(Policy = "ReportsAccess")]
    public async Task<ActionResult<ComprehensiveReportDto>> GetComprehensiveReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? BusinessClock.Today.AddDays(-30);
        var end = endDate ?? BusinessClock.Today;

        if (start > end)
        {
            return BadRequest(new { message = "تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية." });
        }

        var report = await _reportService.GetComprehensiveReportAsync(start, end);
        return Ok(report);
    }

    /// <summary>
    /// الحصول على الأصناف الأكثر مبيعاً
    /// </summary>
    /// <param name="limit">العدد المطلوب عرضه (الافتراضي 10)</param>
    /// <param name="startDate">تاريخ البدء</param>
    /// <param name="endDate">تاريخ النهاية</param>
    [HttpGet("best-selling-products")]
    [Authorize(Policy = "ReportsAccess")]
    public async Task<ActionResult<IEnumerable<ProductReportDto>>> GetBestSellingProducts(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 10)
    {
        var start = startDate ?? BusinessClock.Today.AddDays(-30);
        var end = endDate ?? BusinessClock.Today;

        if (start > end)
        {
            return BadRequest(new { message = "تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية." });
        }

        var products = await _reportService.GetBestSellingProductsAsync(start, end, limit);
        return Ok(products);
    }

    /// <summary>
    /// الحصول على التصنيفات الأكثر مبيعاً
    /// </summary>
    /// <param name="limit">العدد المطلوب عرضه (الافتراضي 10)</param>
    /// <param name="startDate">تاريخ البدء</param>
    /// <param name="endDate">تاريخ النهاية</param>
    [HttpGet("best-selling-categories")]
    [Authorize(Policy = "ReportsAccess")]
    public async Task<ActionResult<IEnumerable<CategoryReportDto>>> GetBestSellingCategories(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 10)
    {
        var start = startDate ?? BusinessClock.Today.AddDays(-30);
        var end = endDate ?? BusinessClock.Today;

        if (start > end)
        {
            return BadRequest(new { message = "تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية." });
        }

        var categories = await _reportService.GetBestSellingCategoriesAsync(start, end, limit);
        return Ok(categories);
    }

    /// <summary>
    /// الحصول على المبيعات اليومية في فترة زمنية
    /// </summary>
    [HttpGet("daily-sales")]
    [Authorize(Policy = "ReportsAccess")]
    public async Task<ActionResult<IEnumerable<DailySalesReportDto>>> GetDailySales(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? BusinessClock.Today.AddDays(-30);
        var end = endDate ?? BusinessClock.Today;

        if (start > end)
        {
            return BadRequest(new { message = "تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية." });
        }

        var dailySales = await _reportService.GetDailySalesAsync(start, end);
        return Ok(dailySales);
    }

    /// <summary>
    /// الحصول على المبيعات مقسمة حسب وسائل الدفع المختلفة
    /// </summary>
    [HttpGet("payment-methods")]
    [Authorize(Policy = "ReportsAccess")]
    public async Task<ActionResult<IEnumerable<PaymentMethodSalesReportDto>>> GetSalesByPaymentMethod(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? BusinessClock.Today.AddDays(-30);
        var end = endDate ?? BusinessClock.Today;

        if (start > end)
        {
            return BadRequest(new { message = "تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية." });
        }

        var salesByPayment = await _reportService.GetSalesByPaymentMethodAsync(start, end);
        return Ok(salesByPayment);
    }

    /// <summary>
    /// الحصول على الطلبات الملغاة وتفاصيلها
    /// </summary>
    [HttpGet("cancelled-orders")]
    [Authorize(Policy = "ReportsAccess")]
    public async Task<ActionResult<IEnumerable<CancelledOrderDto>>> GetCancelledOrders(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? BusinessClock.Today.AddDays(-30);
        var end = endDate ?? BusinessClock.Today;

        if (start > end)
        {
            return BadRequest(new { message = "تاريخ البدء لا يمكن أن يكون بعد تاريخ النهاية." });
        }

        var cancelledOrders = await _reportService.GetCancelledOrdersAsync(start, end);
        return Ok(cancelledOrders);
    }
}
