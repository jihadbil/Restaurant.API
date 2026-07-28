using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IReportApiService _reportApiService;

        private decimal _totalSales;
        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        private decimal _totalProfit;
        public decimal TotalProfit
        {
            get => _totalProfit;
            set => SetProperty(ref _totalProfit, value);
        }

        private int _totalOrders;
        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        private decimal _averageOrderValue;
        public decimal AverageOrderValue
        {
            get => _averageOrderValue;
            set => SetProperty(ref _averageOrderValue, value);
        }

        private DateTime _startDate = DateTime.Today.AddDays(-30);
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public ObservableCollection<BestSellingProductUiModel> BestSellingProducts { get; } = new();
        public bool IsDashboardEmpty => BestSellingProducts.Count == 0;

        public AsyncRelayCommand LoadDataCommand { get; }
        public AsyncRelayCommand ApplyDateFilterCommand { get; }

        public DashboardViewModel(IReportApiService reportApiService)
        {
            _reportApiService = reportApiService;

            LoadDataCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
            ApplyDateFilterCommand = new AsyncRelayCommand(LoadDashboardDataAsync);

            // Trigger load data asynchronously
            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _reportApiService.GetComprehensiveAsync(StartDate, EndDate);
                if (result.IsSuccess && result.Data != null)
                {
                    var data = result.Data;
                    TotalSales = data.TotalSales;
                    TotalProfit = data.TotalProfit;
                    TotalOrders = data.TotalOrdersCount;
                    AverageOrderValue = data.AverageOrderValue;

                    BestSellingProducts.Clear();
                    if (data.BestSellingProducts != null && data.BestSellingProducts.Any())
                    {
                        var maxSold = data.BestSellingProducts.Max(p => p.QuantitySold);
                        foreach (var prod in data.BestSellingProducts)
                        {
                            var uiModel = new BestSellingProductUiModel
                            {
                                Name = prod.Name,
                                QuantitySold = prod.QuantitySold,
                                TotalRevenue = prod.TotalRevenue,
                                TotalProfit = prod.TotalProfit,
                                Percentage = maxSold > 0 ? (double)prod.QuantitySold / maxSold : 0
                            };
                            BestSellingProducts.Add(uiModel);
                        }
                    }
                    OnPropertyChanged(nameof(IsDashboardEmpty));
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تحميل بيانات لوحة القيادة.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class BestSellingProductUiModel
    {
        public string Name { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public double Percentage { get; set; }
        public double InversePercentage => Math.Max(0.01, 1.0 - Percentage);
    }
}
