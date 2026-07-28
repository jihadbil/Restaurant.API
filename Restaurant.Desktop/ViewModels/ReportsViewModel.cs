using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IReportApiService _reportApiService;

        private DateTime _startDate = DateTime.Today.AddDays(-30);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = ExecuteLoadReportAsync();
                }
            }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = ExecuteLoadReportAsync();
                }
            }
        }

        private string _activeTab = "Sales";
        public string ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        // Totals for Sales
        private int _salesTotalOrders;
        public int SalesTotalOrders { get => _salesTotalOrders; set => SetProperty(ref _salesTotalOrders, value); }
        private decimal _salesTotalSales;
        public decimal SalesTotalSales { get => _salesTotalSales; set => SetProperty(ref _salesTotalSales, value); }
        private decimal _salesTotalCost;
        public decimal SalesTotalCost { get => _salesTotalCost; set => SetProperty(ref _salesTotalCost, value); }
        private decimal _salesTotalProfit;
        public decimal SalesTotalProfit { get => _salesTotalProfit; set => SetProperty(ref _salesTotalProfit, value); }

        // Totals for Products
        private int _productsTotalQuantity;
        public int ProductsTotalQuantity { get => _productsTotalQuantity; set => SetProperty(ref _productsTotalQuantity, value); }
        private decimal _productsTotalRevenue;
        public decimal ProductsTotalRevenue { get => _productsTotalRevenue; set => SetProperty(ref _productsTotalRevenue, value); }
        private decimal _productsTotalCost;
        public decimal ProductsTotalCost { get => _productsTotalCost; set => SetProperty(ref _productsTotalCost, value); }
        private decimal _productsTotalProfit;
        public decimal ProductsTotalProfit { get => _productsTotalProfit; set => SetProperty(ref _productsTotalProfit, value); }

        // Totals for Categories
        private int _categoriesTotalQuantity;
        public int CategoriesTotalQuantity { get => _categoriesTotalQuantity; set => SetProperty(ref _categoriesTotalQuantity, value); }
        private decimal _categoriesTotalRevenue;
        public decimal CategoriesTotalRevenue { get => _categoriesTotalRevenue; set => SetProperty(ref _categoriesTotalRevenue, value); }
        private decimal _categoriesTotalCost;
        public decimal CategoriesTotalCost { get => _categoriesTotalCost; set => SetProperty(ref _categoriesTotalCost, value); }
        private decimal _categoriesTotalProfit;
        public decimal CategoriesTotalProfit { get => _categoriesTotalProfit; set => SetProperty(ref _categoriesTotalProfit, value); }

        // Totals for PaymentMethods
        private int _paymentMethodsTotalOrders;
        public int PaymentMethodsTotalOrders { get => _paymentMethodsTotalOrders; set => SetProperty(ref _paymentMethodsTotalOrders, value); }
        private decimal _paymentMethodsTotalSales;
        public decimal PaymentMethodsTotalSales { get => _paymentMethodsTotalSales; set => SetProperty(ref _paymentMethodsTotalSales, value); }

        // Totals for Cancelled Orders
        private decimal _cancelledTotal;
        public decimal CancelledTotal { get => _cancelledTotal; set => SetProperty(ref _cancelledTotal, value); }

        // Data Collections
        public ObservableCollection<DailySalesReportDto> DailySales { get; } = new();
        public ObservableCollection<ProductReportDto> BestProducts { get; } = new();
        public ObservableCollection<CategoryReportDto> BestCategories { get; } = new();
        public ObservableCollection<PaymentMethodSalesReportDto> PaymentMethods { get; } = new();
        public ObservableCollection<CancelledOrderDto> CancelledOrders { get; } = new();

        public AsyncRelayCommand LoadReportCommand { get; }
        public RelayCommand<string> ChangeTabCommand { get; }

        public ReportsViewModel(IReportApiService reportApiService)
        {
            _reportApiService = reportApiService;

            LoadReportCommand = new AsyncRelayCommand(ExecuteLoadReportAsync);
            ChangeTabCommand = new RelayCommand<string>(tab => { if (tab != null) { ActiveTab = tab; _ = ExecuteLoadReportAsync(); } });

            _ = ExecuteLoadReportAsync();
        }

        private async Task ExecuteLoadReportAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                if (ActiveTab == "Sales")
                {
                    var result = await _reportApiService.GetDailySalesAsync(StartDate, EndDate);
                    if (result.IsSuccess && result.Data != null)
                    {
                        DailySales.Clear();
                        foreach (var item in result.Data) DailySales.Add(item);

                        SalesTotalOrders = DailySales.Sum(x => x.TotalOrders);
                        SalesTotalSales = DailySales.Sum(x => x.TotalSales);
                        SalesTotalCost = DailySales.Sum(x => x.TotalCost);
                        SalesTotalProfit = DailySales.Sum(x => x.TotalProfit);
                    }
                    else ErrorMessage = result.ErrorMessage;
                }
                else if (ActiveTab == "Products")
                {
                    var result = await _reportApiService.GetBestSellingProductsAsync(StartDate, EndDate, 15);
                    if (result.IsSuccess && result.Data != null)
                    {
                        BestProducts.Clear();
                        foreach (var item in result.Data) BestProducts.Add(item);

                        ProductsTotalQuantity = BestProducts.Sum(x => x.QuantitySold);
                        ProductsTotalRevenue = BestProducts.Sum(x => x.TotalRevenue);
                        ProductsTotalCost = BestProducts.Sum(x => x.TotalCost);
                        ProductsTotalProfit = BestProducts.Sum(x => x.TotalProfit);
                    }
                    else ErrorMessage = result.ErrorMessage;
                }
                else if (ActiveTab == "Categories")
                {
                    var result = await _reportApiService.GetBestSellingCategoriesAsync(StartDate, EndDate, 15);
                    if (result.IsSuccess && result.Data != null)
                    {
                        BestCategories.Clear();
                        foreach (var item in result.Data) BestCategories.Add(item);

                        CategoriesTotalQuantity = BestCategories.Sum(x => x.QuantitySold);
                        CategoriesTotalRevenue = BestCategories.Sum(x => x.TotalRevenue);
                        CategoriesTotalCost = BestCategories.Sum(x => x.TotalCost);
                        CategoriesTotalProfit = BestCategories.Sum(x => x.TotalProfit);
                    }
                    else ErrorMessage = result.ErrorMessage;
                }
                else if (ActiveTab == "PaymentMethods")
                {
                    var result = await _reportApiService.GetSalesByPaymentMethodAsync(StartDate, EndDate);
                    if (result.IsSuccess && result.Data != null)
                    {
                        PaymentMethods.Clear();
                        foreach (var item in result.Data) PaymentMethods.Add(item);

                        PaymentMethodsTotalOrders = PaymentMethods.Sum(x => x.TotalOrders);
                        PaymentMethodsTotalSales = PaymentMethods.Sum(x => x.TotalSales);
                    }
                    else ErrorMessage = result.ErrorMessage;
                }
                else if (ActiveTab == "Cancelled")
                {
                    var result = await _reportApiService.GetCancelledOrdersAsync(StartDate, EndDate);
                    if (result.IsSuccess && result.Data != null)
                    {
                        CancelledOrders.Clear();
                        foreach (var item in result.Data) CancelledOrders.Add(item);

                        CancelledTotal = CancelledOrders.Sum(x => x.Total);
                    }
                    else ErrorMessage = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"حدث خطأ في تحميل التقرير: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
