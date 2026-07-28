using System;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels;
using Restaurant.Desktop.Views;

namespace Restaurant.Desktop
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Set up Global Exception Handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Start application by showing the LoginWindow
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core
            services.AddSingleton<HttpClient>(provider => new HttpClient());
            services.AddSingleton<ApiClient>();
            services.AddSingleton<SessionManager>(provider => SessionManager.Instance);

            // API Services
            services.AddTransient<IAuthApiService, AuthApiService>();
            services.AddTransient<IUserApiService, UserApiService>();
            services.AddTransient<ICategoryApiService, CategoryApiService>();
            services.AddTransient<IAddonApiService, AddonApiService>();
            services.AddTransient<IProductApiService, ProductApiService>();
            services.AddTransient<IOrderApiService, OrderApiService>();
            services.AddTransient<ICashboxApiService, CashboxApiService>();
            services.AddTransient<ICashDrawerEntryApiService, CashDrawerEntryApiService>();
            services.AddTransient<IPaymentMethodApiService, PaymentMethodApiService>();
            services.AddTransient<IRestaurantApiService, RestaurantApiService>();
            services.AddTransient<IReportApiService, ReportApiService>();
            services.AddTransient<IPrinterApiService, PrinterApiService>();
            services.AddTransient<IPrintStationApiService, PrintStationApiService>();

            // Printing Services
            services.AddSingleton<IWindowsPrintService, WindowsPrintService>();
            // services.AddTransient<IPrintingService, PrintingService>();
            services.AddTransient<IWpfPrintingService, WpfPrintingService>();

            // UI Services
            services.AddSingleton<IToastService, ToastService>();

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<OrdersViewModel>();
            services.AddTransient<NewOrderViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<CategoriesViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<TreasuryViewModel>();
            services.AddTransient<ShiftCloseViewModel>();

            // Windows
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<ShiftCloseWindow>();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"حدث خطأ غير متوقع في النظام:\n{e.Exception.Message}", 
                "خطأ في النظام", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            
            e.Handled = true; // Prevent app crash
        }
    }
}
