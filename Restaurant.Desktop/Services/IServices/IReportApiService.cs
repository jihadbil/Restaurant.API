using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IReportApiService
    {
        Task<ApiResult<ComprehensiveReportDto>> GetComprehensiveAsync(DateTime start, DateTime end);
        Task<ApiResult<List<ProductReportDto>>> GetBestSellingProductsAsync(DateTime start, DateTime end, int limit);
        Task<ApiResult<List<CategoryReportDto>>> GetBestSellingCategoriesAsync(DateTime start, DateTime end, int limit);
        Task<ApiResult<List<DailySalesReportDto>>> GetDailySalesAsync(DateTime start, DateTime end);
        Task<ApiResult<List<PaymentMethodSalesReportDto>>> GetSalesByPaymentMethodAsync(DateTime start, DateTime end);
        Task<ApiResult<List<CancelledOrderDto>>> GetCancelledOrdersAsync(DateTime start, DateTime end);
    }
}
