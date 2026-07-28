using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services
{
    public class ReportApiService : IReportApiService
    {
        private readonly ApiClient _apiClient;

        public ReportApiService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResult<ComprehensiveReportDto>> GetComprehensiveAsync(DateTime start, DateTime end)
        {
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");
            return await _apiClient.GetAsync<ComprehensiveReportDto>($"api/reports/comprehensive?startDate={startStr}&endDate={endStr}");
        }

        public async Task<ApiResult<List<ProductReportDto>>> GetBestSellingProductsAsync(DateTime start, DateTime end, int limit)
        {
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");
            return await _apiClient.GetAsync<List<ProductReportDto>>($"api/reports/best-selling-products?startDate={startStr}&endDate={endStr}&limit={limit}");
        }

        public async Task<ApiResult<List<CategoryReportDto>>> GetBestSellingCategoriesAsync(DateTime start, DateTime end, int limit)
        {
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");
            return await _apiClient.GetAsync<List<CategoryReportDto>>($"api/reports/best-selling-categories?startDate={startStr}&endDate={endStr}&limit={limit}");
        }

        public async Task<ApiResult<List<DailySalesReportDto>>> GetDailySalesAsync(DateTime start, DateTime end)
        {
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");
            return await _apiClient.GetAsync<List<DailySalesReportDto>>($"api/reports/daily-sales?startDate={startStr}&endDate={endStr}");
        }

        public async Task<ApiResult<List<PaymentMethodSalesReportDto>>> GetSalesByPaymentMethodAsync(DateTime start, DateTime end)
        {
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");
            return await _apiClient.GetAsync<List<PaymentMethodSalesReportDto>>($"api/reports/payment-methods?startDate={startStr}&endDate={endStr}");
        }

        public async Task<ApiResult<List<CancelledOrderDto>>> GetCancelledOrdersAsync(DateTime start, DateTime end)
        {
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");
            return await _apiClient.GetAsync<List<CancelledOrderDto>>($"api/reports/cancelled-orders?startDate={startStr}&endDate={endStr}");
        }
    }
}
