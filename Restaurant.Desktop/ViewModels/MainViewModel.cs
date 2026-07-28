using System;
using System.Linq;
using System.Windows;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Desktop.Views;
using Restaurant.Desktop.Services.IServices;

namespace Restaurant.Desktop.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;

        private object? _currentPage;
        public object? CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private string _selectedMenuItem = "Dashboard";
        public string SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem, value))
                {
                    OnPropertyChanged(nameof(CurrentPageTitle));
                }
            }
        }

        public string CurrentPageTitle
        {
            get
            {
                return SelectedMenuItem switch
                {
                    "Dashboard" => "لوحة القيادة والمؤشرات",
                    "NewOrder" => "طلب جديد (نقطة البيع)",
                    "Orders" => "إدارة الطلبات والفواتير",
                    "Products" => "كتالوج المنتجات والأصناف",
                    "Categories" => "تصنيفات المأكولات والمشروبات",
                    "Reports" => "التقارير التفصيلية والأداء",
                    "Treasury" => "حركات الخزينة والصندوق",
                    "Settings" => "إعدادات النظام والطباعة",
                    _ => "الصفحة الرئيسية"
                };
            }
        }

        private bool _isSidebarVisible = true;
        public bool IsSidebarVisible
        {
            get => _isSidebarVisible;
            set => SetProperty(ref _isSidebarVisible, value);
        }

        private string? _restaurantLogoUrl;
        public string? RestaurantLogoUrl
        {
            get => _restaurantLogoUrl;
            set => SetProperty(ref _restaurantLogoUrl, value);
        }

        private string? _restaurantName;
        public string? RestaurantName
        {
            get => _restaurantName;
            set => SetProperty(ref _restaurantName, value);
        }

        public string CurrentUserName => SessionManager.Instance.CurrentUser?.UserName ?? "مستخدم";

        public bool IsAdmin => SessionManager.Instance.CurrentUser?.Roles != null && 
                               SessionManager.Instance.CurrentUser.Roles.Contains("Admin");

        public bool CanAccessDashboard => HasPermission("Permission.Dashboard");
        public bool CanAccessNewOrder => HasPermission("Permission.NewOrder");
        public bool CanAccessOrders => HasPermission("Permission.Orders");
        public bool CanAccessProducts => HasPermission("Permission.Products");
        public bool CanAccessCategories => HasPermission("Permission.Categories");
        public bool CanAccessReports => HasPermission("Permission.Reports");
        public bool CanAccessSettings => HasPermission("Permission.Settings");
        public bool CanAccessTreasury => HasPermission("Permission.Treasury");

        private bool HasPermission(string permissionName)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            // Admin bypasses all page permission checks
            if (user.Roles != null && user.Roles.Contains("Admin")) return true;
            return user.Permissions != null && user.Permissions.Contains(permissionName);
        }

        public string CurrentUserRoleDisplay
        {
            get
            {
                var user = SessionManager.Instance.CurrentUser;
                if (user == null) return "مستخدم";
                if (user.Roles != null)
                {
                    if (user.Roles.Contains("Admin")) return "مدير النظام";
                    if (user.Roles.Contains("Cashier")) return "كاشير";
                    return string.Join(", ", user.Roles);
                }
                return "مستخدم";
            }
        }

        public RelayCommand NavigateToDashboardCommand { get; }
        public RelayCommand NavigateToOrdersCommand { get; }
        public RelayCommand NavigateToNewOrderCommand { get; }
        public RelayCommand NavigateToProductsCommand { get; }
        public RelayCommand NavigateToCategoriesCommand { get; }
        public RelayCommand NavigateToReportsCommand { get; }
        public RelayCommand NavigateToSettingsCommand { get; }
        public RelayCommand NavigateToTreasuryCommand { get; }
        public RelayCommand OpenShiftCloseCommand { get; }
        public RelayCommand ToggleSidebarCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            NavigateToDashboardCommand = new RelayCommand(() => Navigate("Dashboard", typeof(DashboardViewModel)));
            NavigateToOrdersCommand = new RelayCommand(() => Navigate("Orders", typeof(OrdersViewModel)));
            NavigateToNewOrderCommand = new RelayCommand(() => Navigate("NewOrder", typeof(NewOrderViewModel)));
            NavigateToProductsCommand = new RelayCommand(() => Navigate("Products", typeof(ProductsViewModel)));
            NavigateToCategoriesCommand = new RelayCommand(() => Navigate("Categories", typeof(CategoriesViewModel)));
            NavigateToReportsCommand = new RelayCommand(() => Navigate("Reports", typeof(ReportsViewModel)));
            NavigateToSettingsCommand = new RelayCommand(() => Navigate("Settings", typeof(SettingsViewModel)));
            NavigateToTreasuryCommand = new RelayCommand(() => Navigate("Treasury", typeof(TreasuryViewModel)));
            OpenShiftCloseCommand = new RelayCommand(ExecuteOpenShiftClose);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarVisible = !IsSidebarVisible);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            LoadRestaurantInfo();

            // Start on first permitted page
            if (CanAccessDashboard)
            {
                Navigate("Dashboard", typeof(DashboardViewModel));
            }
            else if (CanAccessNewOrder)
            {
                Navigate("NewOrder", typeof(NewOrderViewModel));
            }
            else if (CanAccessOrders)
            {
                Navigate("Orders", typeof(OrdersViewModel));
            }
            else if (CanAccessProducts)
            {
                Navigate("Products", typeof(ProductsViewModel));
            }
            else if (CanAccessCategories)
            {
                Navigate("Categories", typeof(CategoriesViewModel));
            }
            else if (CanAccessReports)
            {
                Navigate("Reports", typeof(ReportsViewModel));
            }
            else if (CanAccessTreasury)
            {
                Navigate("Treasury", typeof(TreasuryViewModel));
            }
            else if (CanAccessSettings)
            {
                Navigate("Settings", typeof(SettingsViewModel));
            }
            else
            {
                // Fallback (no access to any pages)
                Navigate("NewOrder", typeof(NewOrderViewModel));
            }
        }

        private void Navigate(string menuName, Type viewModelType)
        {
            SelectedMenuItem = menuName;
            var vm = _serviceProvider.GetService(viewModelType);
            if (vm != null)
            {
                CurrentPage = vm;
            }
        }

        private void ExecuteLogout()
        {
            var owner = Application.Current.MainWindow;
            var confirmed = Restaurant.Desktop.Controls.ConfirmWindow.Show(
                owner,
                "تسجيل الخروج",
                "هل أنت متأكد من رغبتك في تسجيل الخروج والعودة لشاشة الدخول؟",
                "نعم، سجل الخروج",
                "primary");

            if (!confirmed) return;

            SessionManager.Instance.ClearSession();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var loginWindow = _serviceProvider.GetService(typeof(LoginWindow)) as LoginWindow;
                if (loginWindow != null)
                {
                    loginWindow.Show();

                    // Close main window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow)
                        {
                            window.Close();
                            break;
                        }
                    }
                }
            });
        }

        private void ExecuteOpenShiftClose()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var shiftCloseWindow = _serviceProvider.GetService(typeof(ShiftCloseWindow)) as ShiftCloseWindow;
                shiftCloseWindow?.ShowDialog();
            });
        }

        private async void LoadRestaurantInfo()
        {
            try
            {
                var restaurantId = SessionManager.Instance.CurrentUser?.RestaurantId;
                if (restaurantId.HasValue)
                {
                    var restaurantApiService = (IRestaurantApiService?)_serviceProvider.GetService(typeof(IRestaurantApiService));
                    if (restaurantApiService != null)
                    {
                        var result = await restaurantApiService.GetByIdAsync(restaurantId.Value);
                        if (result != null && result.IsSuccess && result.Data != null)
                        {
                            RestaurantLogoUrl = result.Data.LogoUrl;
                            RestaurantName = result.Data.Name;
                        }
                    }
                }
            }
            catch
            {
                // Ignore load error
            }
        }
    }
}
