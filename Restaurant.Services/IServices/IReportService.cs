using Restaurant.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

/// <summary>
/// واجهة خدمة التقارير والإحصائيات
/// </summary>
public interface IReportService
{
    /// <summary>
    /// الحصول على التقرير الشامل للمؤشرات والملخصات لفلترة زمنية معينة
    /// </summary>
    Task<ComprehensiveReportDto> GetComprehensiveReportAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// الحصول على قائمة الأصناف الأكثر مبيعاً
    /// </summary>
    Task<IEnumerable<ProductReportDto>> GetBestSellingProductsAsync(DateTime startDate, DateTime endDate, int limit = 10);

    /// <summary>
    /// الحصول على قائمة التصنيفات الأكثر مبيعاً
    /// </summary>
    Task<IEnumerable<CategoryReportDto>> GetBestSellingCategoriesAsync(DateTime startDate, DateTime endDate, int limit = 10);

    /// <summary>
    /// الحصول على إحصائيات المبيعات اليومية
    /// </summary>
    Task<IEnumerable<DailySalesReportDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// الحصول على إحصائيات المبيعات مقسمة حسب وسيلة الدفع
    /// </summary>
    Task<IEnumerable<PaymentMethodSalesReportDto>> GetSalesByPaymentMethodAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// الحصول على قائمة الطلبات التي تم إلغاؤها
    /// </summary>
    Task<IEnumerable<CancelledOrderDto>> GetCancelledOrdersAsync(DateTime startDate, DateTime endDate);
}
