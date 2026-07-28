using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IPrinterApiService _printerApiService;
        private readonly IPrintStationApiService _printStationApiService;
        private readonly ICashboxApiService _cashboxApiService;
        private readonly IAddonApiService _addonApiService;
        private readonly IPaymentMethodApiService _paymentMethodApiService;
        private readonly IRestaurantApiService _restaurantApiService;
        private readonly IUserApiService _userApiService;

        private string _activeTab = "Stations"; // Stations or Printers
        public string ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        // Data Collections
        public ObservableCollection<PrintStationDto> PrintStations { get; } = new();
        public ObservableCollection<PrinterDto> Printers { get; } = new();
        public ObservableCollection<CashboxDto> Cashboxes { get; } = new();
        public ObservableCollection<AddonDto> Addons { get; } = new();
        public ObservableCollection<PaymentMethodDto> PaymentMethods { get; } = new();
        public ObservableCollection<UserDto> Users { get; } = new();
        public ObservableCollection<string> AvailableRoles { get; } = new();

        // Enum lists for ComboBox
        public List<PrinterType> AvailablePrinterTypes { get; } = new()
        {
            PrinterType.Receipt,
            PrinterType.Kitchen,
            PrinterType.Report
        };

        // Installed Windows Printers
        public ObservableCollection<string> InstalledPrinters { get; } = new();

        // --- User Form Properties ---
        private bool _isUserFormVisible;
        public bool IsUserFormVisible
        {
            get => _isUserFormVisible;
            set => SetProperty(ref _isUserFormVisible, value);
        }

        private bool _isUserEditMode;
        public bool IsUserEditMode
        {
            get => _isUserEditMode;
            set => SetProperty(ref _isUserEditMode, value);
        }

        private string _userFormUsername = string.Empty;
        public string UserFormUsername
        {
            get => _userFormUsername;
            set
            {
                if (SetProperty(ref _userFormUsername, value))
                {
                    SaveUserCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _userFormEmail = string.Empty;
        public string UserFormEmail
        {
            get => _userFormEmail;
            set
            {
                if (SetProperty(ref _userFormEmail, value))
                {
                    SaveUserCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _userFormPhoneNumber = string.Empty;
        public string UserFormPhoneNumber
        {
            get => _userFormPhoneNumber;
            set => SetProperty(ref _userFormPhoneNumber, value);
        }

        private string _userFormPassword = string.Empty;
        public string UserFormPassword
        {
            get => _userFormPassword;
            set
            {
                if (SetProperty(ref _userFormPassword, value))
                {
                    SaveUserCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _userFormIsAdmin;
        public bool UserFormIsAdmin
        {
            get => _userFormIsAdmin;
            set => SetProperty(ref _userFormIsAdmin, value);
        }

        private bool _userFormIsCashier;
        public bool UserFormIsCashier
        {
            get => _userFormIsCashier;
            set => SetProperty(ref _userFormIsCashier, value);
        }

        private bool _userFormPermDashboard;
        public bool UserFormPermDashboard
        {
            get => _userFormPermDashboard;
            set => SetProperty(ref _userFormPermDashboard, value);
        }

        private bool _userFormPermNewOrder;
        public bool UserFormPermNewOrder
        {
            get => _userFormPermNewOrder;
            set => SetProperty(ref _userFormPermNewOrder, value);
        }

        private bool _userFormPermOrders;
        public bool UserFormPermOrders
        {
            get => _userFormPermOrders;
            set => SetProperty(ref _userFormPermOrders, value);
        }

        private bool _userFormPermProducts;
        public bool UserFormPermProducts
        {
            get => _userFormPermProducts;
            set => SetProperty(ref _userFormPermProducts, value);
        }

        private bool _userFormPermCategories;
        public bool UserFormPermCategories
        {
            get => _userFormPermCategories;
            set => SetProperty(ref _userFormPermCategories, value);
        }

        private bool _userFormPermReports;
        public bool UserFormPermReports
        {
            get => _userFormPermReports;
            set => SetProperty(ref _userFormPermReports, value);
        }

        private bool _userFormPermSettings;
        public bool UserFormPermSettings
        {
            get => _userFormPermSettings;
            set => SetProperty(ref _userFormPermSettings, value);
        }

        private bool _userFormPermTreasury;
        public bool UserFormPermTreasury
        {
            get => _userFormPermTreasury;
            set => SetProperty(ref _userFormPermTreasury, value);
        }

        private bool _userFormPermPOSApplyDiscount;
        public bool UserFormPermPOSApplyDiscount
        {
            get => _userFormPermPOSApplyDiscount;
            set => SetProperty(ref _userFormPermPOSApplyDiscount, value);
        }

        private bool _userFormPermPOSVoidItem;
        public bool UserFormPermPOSVoidItem
        {
            get => _userFormPermPOSVoidItem;
            set => SetProperty(ref _userFormPermPOSVoidItem, value);
        }

        private bool _userFormPermOrdersVoidOrder;
        public bool UserFormPermOrdersVoidOrder
        {
            get => _userFormPermOrdersVoidOrder;
            set => SetProperty(ref _userFormPermOrdersVoidOrder, value);
        }

        private bool _userFormPermShiftViewTotals;
        public bool UserFormPermShiftViewTotals
        {
            get => _userFormPermShiftViewTotals;
            set => SetProperty(ref _userFormPermShiftViewTotals, value);
        }

        private bool _userFormPermProductsManage;
        public bool UserFormPermProductsManage
        {
            get => _userFormPermProductsManage;
            set => SetProperty(ref _userFormPermProductsManage, value);
        }

        private bool _userFormPermCategoriesManage;
        public bool UserFormPermCategoriesManage
        {
            get => _userFormPermCategoriesManage;
            set => SetProperty(ref _userFormPermCategoriesManage, value);
        }

        private UserDto? _selectedUser;
        public UserDto? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        // --- User Password Form Properties ---
        private bool _isPasswordFormVisible;
        public bool IsPasswordFormVisible
        {
            get => _isPasswordFormVisible;
            set => SetProperty(ref _isPasswordFormVisible, value);
        }

        private string _passwordFormNewPassword = string.Empty;
        public string PasswordFormNewPassword
        {
            get => _passwordFormNewPassword;
            set
            {
                if (SetProperty(ref _passwordFormNewPassword, value))
                {
                    SavePasswordCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Print Station Form Properties ---
        private bool _isStationFormVisible;
        public bool IsStationFormVisible
        {
            get => _isStationFormVisible;
            set => SetProperty(ref _isStationFormVisible, value);
        }

        private bool _isStationEditMode;
        public bool IsStationEditMode
        {
            get => _isStationEditMode;
            set => SetProperty(ref _isStationEditMode, value);
        }

        private string _stationFormName = string.Empty;
        public string StationFormName
        {
            get => _stationFormName;
            set
            {
                if (SetProperty(ref _stationFormName, value))
                {
                    SaveStationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private PrintStationDto? _selectedStation;
        public PrintStationDto? SelectedStation
        {
            get => _selectedStation;
            set => SetProperty(ref _selectedStation, value);
        }

        // --- Printer Form Properties ---
        private bool _isPrinterFormVisible;
        public bool IsPrinterFormVisible
        {
            get => _isPrinterFormVisible;
            set => SetProperty(ref _isPrinterFormVisible, value);
        }

        private bool _isPrinterEditMode;
        public bool IsPrinterEditMode
        {
            get => _isPrinterEditMode;
            set => SetProperty(ref _isPrinterEditMode, value);
        }

        private string _printerFormName = string.Empty;
        public string PrinterFormName
        {
            get => _printerFormName;
            set
            {
                if (SetProperty(ref _printerFormName, value))
                {
                    SavePrinterCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _printerFormRealName = string.Empty;
        public string PrinterFormRealName
        {
            get => _printerFormRealName;
            set
            {
                if (SetProperty(ref _printerFormRealName, value))
                {
                    SavePrinterCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private PrinterType _printerFormType = PrinterType.Receipt;
        public PrinterType PrinterFormType
        {
            get => _printerFormType;
            set => SetProperty(ref _printerFormType, value);
        }

        private int _printerFormStationId;
        public int PrinterFormStationId
        {
            get => _printerFormStationId;
            set
            {
                if (SetProperty(ref _printerFormStationId, value))
                {
                    SavePrinterCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private PrinterDto? _selectedPrinter;
        public PrinterDto? SelectedPrinter
        {
            get => _selectedPrinter;
            set => SetProperty(ref _selectedPrinter, value);
        }

        // --- Cashbox Form Properties ---
        private bool _isCashboxFormVisible;
        public bool IsCashboxFormVisible
        {
            get => _isCashboxFormVisible;
            set => SetProperty(ref _isCashboxFormVisible, value);
        }

        private bool _isCashboxEditMode;
        public bool IsCashboxEditMode
        {
            get => _isCashboxEditMode;
            set
            {
                if (SetProperty(ref _isCashboxEditMode, value))
                {
                    OnPropertyChanged(nameof(IsInitialBalanceEnabled));
                }
            }
        }

        public bool IsInitialBalanceEnabled => !IsCashboxEditMode;

        private string _cashboxFormName = string.Empty;
        public string CashboxFormName
        {
            get => _cashboxFormName;
            set
            {
                if (SetProperty(ref _cashboxFormName, value))
                {
                    SaveCashboxCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _cashboxFormDescription;
        public string? CashboxFormDescription
        {
            get => _cashboxFormDescription;
            set => SetProperty(ref _cashboxFormDescription, value);
        }

        private decimal _cashboxFormInitialBalance;
        public decimal CashboxFormInitialBalance
        {
            get => _cashboxFormInitialBalance;
            set => SetProperty(ref _cashboxFormInitialBalance, value);
        }

        private bool _cashboxFormIsActive = true;
        public bool CashboxFormIsActive
        {
            get => _cashboxFormIsActive;
            set => SetProperty(ref _cashboxFormIsActive, value);
        }

        private CashboxDto? _selectedCashbox;
        public CashboxDto? SelectedCashbox
        {
            get => _selectedCashbox;
            set => SetProperty(ref _selectedCashbox, value);
        }

        private int? _settingsDefaultCashboxId;
        public int? SettingsDefaultCashboxId
        {
            get => _settingsDefaultCashboxId;
            set => SetProperty(ref _settingsDefaultCashboxId, value);
        }

        // --- Addon Form Properties ---
        private bool _isAddonFormVisible;
        public bool IsAddonFormVisible
        {
            get => _isAddonFormVisible;
            set => SetProperty(ref _isAddonFormVisible, value);
        }

        private bool _isAddonEditMode;
        public bool IsAddonEditMode
        {
            get => _isAddonEditMode;
            set => SetProperty(ref _isAddonEditMode, value);
        }

        private string _addonFormName = string.Empty;
        public string AddonFormName
        {
            get => _addonFormName;
            set
            {
                if (SetProperty(ref _addonFormName, value))
                {
                    SaveAddonCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private AddonDto? _selectedAddon;
        public AddonDto? SelectedAddon
        {
            get => _selectedAddon;
            set => SetProperty(ref _selectedAddon, value);
        }

        // --- General Settings Properties ---
        private string _generalApiBaseUrl = string.Empty;
        public string GeneralApiBaseUrl
        {
            get => _generalApiBaseUrl;
            set => SetProperty(ref _generalApiBaseUrl, value);
        }

        private OrderStatus _generalDefaultOrderStatus = OrderStatus.Preparing;
        public OrderStatus GeneralDefaultOrderStatus
        {
            get => _generalDefaultOrderStatus;
            set => SetProperty(ref _generalDefaultOrderStatus, value);
        }

        public List<OrderStatus> AvailableOrderStatuses { get; } = new()
        {
            OrderStatus.Preparing,
            OrderStatus.Ready,
            OrderStatus.Delivered
        };

        // --- Payment Method Form Properties ---
        private bool _isPaymentMethodFormVisible;
        public bool IsPaymentMethodFormVisible
        {
            get => _isPaymentMethodFormVisible;
            set => SetProperty(ref _isPaymentMethodFormVisible, value);
        }

        private bool _isPaymentMethodEditMode;
        public bool IsPaymentMethodEditMode
        {
            get => _isPaymentMethodEditMode;
            set => SetProperty(ref _isPaymentMethodEditMode, value);
        }

        private string _paymentMethodFormName = string.Empty;
        public string PaymentMethodFormName
        {
            get => _paymentMethodFormName;
            set
            {
                if (SetProperty(ref _paymentMethodFormName, value))
                {
                    SavePaymentMethodCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _paymentMethodFormIsTaxFree;
        public bool PaymentMethodFormIsTaxFree
        {
            get => _paymentMethodFormIsTaxFree;
            set => SetProperty(ref _paymentMethodFormIsTaxFree, value);
        }

        private string? _paymentMethodFormLogoUrl;
        public string? PaymentMethodFormLogoUrl
        {
            get => _paymentMethodFormLogoUrl;
            set => SetProperty(ref _paymentMethodFormLogoUrl, value);
        }

        private PaymentMethodDto? _selectedPaymentMethod;
        public PaymentMethodDto? SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        // --- Restaurant Info Properties ---
        private RestaurantDto? _currentRestaurant;
        public RestaurantDto? CurrentRestaurant
        {
            get => _currentRestaurant;
            set
            {
                if (SetProperty(ref _currentRestaurant, value))
                {
                    OnPropertyChanged(nameof(HasRestaurant));
                }
            }
        }

        public bool HasRestaurant => CurrentRestaurant != null;

        private string _restaurantFormName = string.Empty;
        public string RestaurantFormName
        {
            get => _restaurantFormName;
            set
            {
                if (SetProperty(ref _restaurantFormName, value))
                {
                    SaveRestaurantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _restaurantFormAddress;
        public string? RestaurantFormAddress
        {
            get => _restaurantFormAddress;
            set => SetProperty(ref _restaurantFormAddress, value);
        }

        private string? _restaurantFormPhoneNumber;
        public string? RestaurantFormPhoneNumber
        {
            get => _restaurantFormPhoneNumber;
            set => SetProperty(ref _restaurantFormPhoneNumber, value);
        }

        private string? _restaurantFormTaxNumber;
        public string? RestaurantFormTaxNumber
        {
            get => _restaurantFormTaxNumber;
            set => SetProperty(ref _restaurantFormTaxNumber, value);
        }

        private string? _restaurantFormLogoUrl;
        public string? RestaurantFormLogoUrl
        {
            get => _restaurantFormLogoUrl;
            set => SetProperty(ref _restaurantFormLogoUrl, value);
        }

        // Commands
        public AsyncRelayCommand LoadDataCommand { get; }
        public RelayCommand<string> ChangeTabCommand { get; }

        // Station commands
        public RelayCommand ShowAddStationFormCommand { get; }
        public RelayCommand CancelStationFormCommand { get; }
        public RelayCommand<PrintStationDto> EditStationCommand { get; }
        public AsyncRelayCommand SaveStationCommand { get; }
        public AsyncRelayCommand<int> DeleteStationCommand { get; }

        // Printer commands
        public RelayCommand ShowAddPrinterFormCommand { get; }
        public RelayCommand CancelPrinterFormCommand { get; }
        public RelayCommand<PrinterDto> EditPrinterCommand { get; }
        public AsyncRelayCommand SavePrinterCommand { get; }
        public AsyncRelayCommand<int> DeletePrinterCommand { get; }

        // General Settings Command
        public RelayCommand SaveGeneralSettingsCommand { get; }

        // Cashbox commands
        public RelayCommand ShowAddCashboxFormCommand { get; }
        public RelayCommand CancelCashboxFormCommand { get; }
        public RelayCommand<CashboxDto> EditCashboxCommand { get; }
        public AsyncRelayCommand SaveCashboxCommand { get; }
        public AsyncRelayCommand<int> DeleteCashboxCommand { get; }
        public RelayCommand SaveDefaultCashboxCommand { get; }

        // Addon commands
        public RelayCommand ShowAddAddonFormCommand { get; }
        public RelayCommand CancelAddonFormCommand { get; }
        public RelayCommand<AddonDto> EditAddonCommand { get; }
        public AsyncRelayCommand SaveAddonCommand { get; }
        public AsyncRelayCommand<int> DeleteAddonCommand { get; }

        // Payment Method commands
        public RelayCommand ShowAddPaymentMethodFormCommand { get; }
        public RelayCommand CancelPaymentMethodFormCommand { get; }
        public RelayCommand<PaymentMethodDto> EditPaymentMethodCommand { get; }
        public AsyncRelayCommand SavePaymentMethodCommand { get; }
        public AsyncRelayCommand<int> DeletePaymentMethodCommand { get; }
        public AsyncRelayCommand SelectPaymentMethodLogoCommand { get; }

        // Restaurant commands
        public AsyncRelayCommand SaveRestaurantCommand { get; }
        public AsyncRelayCommand CreateRestaurantCommand { get; }
        public AsyncRelayCommand SelectRestaurantLogoCommand { get; }

        // User commands
        public RelayCommand ShowAddUserFormCommand { get; }
        public RelayCommand CancelUserFormCommand { get; }
        public RelayCommand<UserDto> EditUserCommand { get; }
        public AsyncRelayCommand SaveUserCommand { get; }
        public AsyncRelayCommand<string> DeleteUserCommand { get; }
        public RelayCommand<UserDto> ChangeUserPasswordCommand { get; }
        public AsyncRelayCommand SavePasswordCommand { get; }
        public RelayCommand CancelPasswordFormCommand { get; }

        public SettingsViewModel(
            IPrinterApiService printerApiService, 
            IPrintStationApiService printStationApiService, 
            ICashboxApiService cashboxApiService, 
            IAddonApiService addonApiService,
            IPaymentMethodApiService paymentMethodApiService,
            IRestaurantApiService restaurantApiService,
            IUserApiService userApiService)
        {
            _printerApiService = printerApiService;
            _printStationApiService = printStationApiService;
            _cashboxApiService = cashboxApiService;
            _addonApiService = addonApiService;
            _paymentMethodApiService = paymentMethodApiService;
            _restaurantApiService = restaurantApiService;

            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            ChangeTabCommand = new RelayCommand<string>(tab => { if (tab != null) ActiveTab = tab; });

            // Station Commands
            ShowAddStationFormCommand = new RelayCommand(ShowAddStationForm);
            CancelStationFormCommand = new RelayCommand(CancelStationForm);
            EditStationCommand = new RelayCommand<PrintStationDto>(EditStation);
            SaveStationCommand = new AsyncRelayCommand(SaveStationAsync, CanSaveStation);
            DeleteStationCommand = new AsyncRelayCommand<int>(DeleteStationAsync);

            // Printer Commands
            ShowAddPrinterFormCommand = new RelayCommand(ShowAddPrinterForm);
            CancelPrinterFormCommand = new RelayCommand(CancelPrinterForm);
            EditPrinterCommand = new RelayCommand<PrinterDto>(EditPrinter);
            SavePrinterCommand = new AsyncRelayCommand(SavePrinterAsync, CanSavePrinter);
            DeletePrinterCommand = new AsyncRelayCommand<int>(DeletePrinterAsync);

            // Cashbox Commands
            ShowAddCashboxFormCommand = new RelayCommand(ShowAddCashboxForm);
            CancelCashboxFormCommand = new RelayCommand(CancelCashboxForm);
            EditCashboxCommand = new RelayCommand<CashboxDto>(EditCashbox);
            SaveCashboxCommand = new AsyncRelayCommand(SaveCashboxAsync, CanSaveCashbox);
            DeleteCashboxCommand = new AsyncRelayCommand<int>(DeleteCashboxAsync);
            SaveDefaultCashboxCommand = new RelayCommand(SaveDefaultCashbox);

            // General Settings Command
            SaveGeneralSettingsCommand = new RelayCommand(SaveGeneralSettings);

            // Addon Commands
            ShowAddAddonFormCommand = new RelayCommand(ShowAddAddonForm);
            CancelAddonFormCommand = new RelayCommand(CancelAddonForm);
            EditAddonCommand = new RelayCommand<AddonDto>(EditAddon);
            SaveAddonCommand = new AsyncRelayCommand(SaveAddonAsync, CanSaveAddon);
            DeleteAddonCommand = new AsyncRelayCommand<int>(DeleteAddonAsync);

            // Payment Method Commands
            ShowAddPaymentMethodFormCommand = new RelayCommand(ShowAddPaymentMethodForm);
            CancelPaymentMethodFormCommand = new RelayCommand(CancelPaymentMethodForm);
            EditPaymentMethodCommand = new RelayCommand<PaymentMethodDto>(EditPaymentMethod);
            SavePaymentMethodCommand = new AsyncRelayCommand(SavePaymentMethodAsync, CanSavePaymentMethod);
            DeletePaymentMethodCommand = new AsyncRelayCommand<int>(DeletePaymentMethodAsync);
            SelectPaymentMethodLogoCommand = new AsyncRelayCommand(SelectPaymentMethodLogoAsync);

            // Restaurant Commands
            SaveRestaurantCommand = new AsyncRelayCommand(SaveRestaurantAsync, CanSaveRestaurant);
            CreateRestaurantCommand = new AsyncRelayCommand(CreateRestaurantAsync, CanSaveRestaurant);
            SelectRestaurantLogoCommand = new AsyncRelayCommand(SelectRestaurantLogoAsync);

            // User Commands
            ShowAddUserFormCommand = new RelayCommand(ShowAddUserForm);
            CancelUserFormCommand = new RelayCommand(CancelUserForm);
            EditUserCommand = new RelayCommand<UserDto>(EditUser);
            SaveUserCommand = new AsyncRelayCommand(SaveUserAsync, CanSaveUser);
            DeleteUserCommand = new AsyncRelayCommand<string>(DeleteUserAsync);
            ChangeUserPasswordCommand = new RelayCommand<UserDto>(ChangeUserPassword);
            SavePasswordCommand = new AsyncRelayCommand(SavePasswordAsync, CanSavePassword);
            CancelPasswordFormCommand = new RelayCommand(CancelPasswordForm);

            _userApiService = userApiService;

            _ = LoadDataAsync();
        }

        private void SaveGeneralSettings()
        {
            try
            {
                AppSettings.Instance.ApiBaseUrl = GeneralApiBaseUrl;
                AppSettings.Instance.DefaultOrderStatus = GeneralDefaultOrderStatus;
                AppSettings.Instance.Save();
                System.Windows.MessageBox.Show("تم حفظ الإعدادات العامة بنجاح!", "حفظ الإعدادات", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ أثناء حفظ الإعدادات: {ex.Message}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task LoadDataAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                LoadInstalledPrinters();
                GeneralApiBaseUrl = AppSettings.Instance.ApiBaseUrl;
                GeneralDefaultOrderStatus = AppSettings.Instance.DefaultOrderStatus;
                SettingsDefaultCashboxId = AppSettings.Instance.DefaultCashboxId;

                var stationsResult = await _printStationApiService.GetAllAsync();
                if (stationsResult.IsSuccess && stationsResult.Data != null)
                {
                    PrintStations.Clear();
                    foreach (var s in stationsResult.Data) PrintStations.Add(s);
                }

                var printersResult = await _printerApiService.GetAllAsync();
                if (printersResult.IsSuccess && printersResult.Data != null)
                {
                    Printers.Clear();
                    foreach (var p in printersResult.Data) Printers.Add(p);
                }

                var cashboxesResult = await _cashboxApiService.GetAllAsync();
                if (cashboxesResult.IsSuccess && cashboxesResult.Data != null)
                {
                    Cashboxes.Clear();
                    foreach (var c in cashboxesResult.Data) Cashboxes.Add(c);
                }

                var addonsResult = await _addonApiService.GetAllAsync();
                if (addonsResult.IsSuccess && addonsResult.Data != null)
                {
                    Addons.Clear();
                    foreach (var a in addonsResult.Data) Addons.Add(a);
                }

                var paymentMethodsResult = await _paymentMethodApiService.GetAllAsync();
                if (paymentMethodsResult.IsSuccess && paymentMethodsResult.Data != null)
                {
                    PaymentMethods.Clear();
                    foreach (var pm in paymentMethodsResult.Data) PaymentMethods.Add(pm);
                }

                // Load Current User's Restaurant
                var restaurantId = SessionManager.Instance.CurrentUser?.RestaurantId;
                if (restaurantId.HasValue)
                {
                    var restaurantResult = await _restaurantApiService.GetByIdAsync(restaurantId.Value);
                    if (restaurantResult.IsSuccess && restaurantResult.Data != null)
                    {
                        CurrentRestaurant = restaurantResult.Data;
                        RestaurantFormName = CurrentRestaurant.Name;
                        RestaurantFormAddress = CurrentRestaurant.Address;
                        RestaurantFormPhoneNumber = CurrentRestaurant.PhoneNumber;
                        RestaurantFormTaxNumber = CurrentRestaurant.TaxNumber;
                        RestaurantFormLogoUrl = CurrentRestaurant.LogoUrl;
                    }
                    else
                    {
                        CurrentRestaurant = null;
                    }
                }
                else
                {
                    CurrentRestaurant = null;
                    // Reset form fields
                    RestaurantFormName = string.Empty;
                    RestaurantFormAddress = string.Empty;
                    RestaurantFormPhoneNumber = string.Empty;
                    RestaurantFormTaxNumber = string.Empty;
                    RestaurantFormLogoUrl = string.Empty;
                }

                // Load Users & Roles if Admin
                var isUserAdmin = SessionManager.Instance.CurrentUser?.Roles != null && 
                                  SessionManager.Instance.CurrentUser.Roles.Contains("Admin");
                if (isUserAdmin)
                {
                    var usersResult = await _userApiService.GetUsersAsync();
                    if (usersResult.IsSuccess && usersResult.Data != null)
                    {
                        Users.Clear();
                        foreach (var u in usersResult.Data) Users.Add(u);
                    }

                    var rolesResult = await _userApiService.GetAvailableRolesAsync();
                    if (rolesResult.IsSuccess && rolesResult.Data != null)
                    {
                        AvailableRoles.Clear();
                        foreach (var r in rolesResult.Data) AvailableRoles.Add(r);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ في جلب الإعدادات: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadInstalledPrinters()
        {
            try
            {
                InstalledPrinters.Clear();
                foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    InstalledPrinters.Add(printerName);
                }
                
                // If the list is empty, add a default fallback
                if (!InstalledPrinters.Any())
                {
                    InstalledPrinters.Add("طابعة النظام الافتراضية");
                }
            }
            catch (Exception)
            {
                InstalledPrinters.Clear();
                InstalledPrinters.Add("طابعة النظام الافتراضية");
            }
        }

        // --- Station Logical Actions ---
        private void ShowAddStationForm()
        {
            SelectedStation = null;
            StationFormName = string.Empty;
            IsStationEditMode = false;
            IsStationFormVisible = true;
        }

        private void CancelStationForm()
        {
            SelectedStation = null;
            StationFormName = string.Empty;
            IsStationFormVisible = false;
        }

        private void EditStation(PrintStationDto? station)
        {
            if (station == null) return;
            SelectedStation = station;
            StationFormName = station.Name;
            IsStationEditMode = true;
            IsStationFormVisible = true;
        }

        private bool CanSaveStation() => !IsBusy && !string.IsNullOrWhiteSpace(StationFormName);

        private async Task SaveStationAsync()
        {
            if (string.IsNullOrWhiteSpace(StationFormName)) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsStationEditMode && SelectedStation != null)
                {
                    var dto = new PrintStationUpdateDto { Id = SelectedStation.Id, Name = StationFormName };
                    var result = await _printStationApiService.UpdateAsync(SelectedStation.Id, dto);
                    if (result.IsSuccess)
                    {
                        var s = PrintStations.FirstOrDefault(st => st.Id == SelectedStation.Id);
                        if (s != null) s.Name = StationFormName;
                        CancelStationForm();
                        _ = LoadDataAsync(); // Refresh dependencies/names
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل تعديل المحطة.";
                }
                else
                {
                    var dto = new PrintStationCreateDto { Name = StationFormName };
                    var result = await _printStationApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        PrintStations.Add(result.Data);
                        CancelStationForm();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل إضافة المحطة.";
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

        private async Task DeleteStationAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف محطة الطباعة هذه؟ قد يؤدي ذلك لإلغاء ربط طابعاتها والتصنيفات المرتبطة بها.",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _printStationApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var s = PrintStations.FirstOrDefault(st => st.Id == id);
                    if (s != null) PrintStations.Remove(s);
                    CancelStationForm();
                    _ = LoadDataAsync(); // Refresh printer configurations/names
                }
                else ErrorMessage = result.ErrorMessage ?? "فشل حذف المحطة.";
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

        // --- Printer Logical Actions ---
        private void ShowAddPrinterForm()
        {
            SelectedPrinter = null;
            PrinterFormName = string.Empty;
            PrinterFormRealName = string.Empty;
            PrinterFormType = PrinterType.Receipt;
            if (PrintStations.Any())
            {
                PrinterFormStationId = PrintStations.First().Id;
            }
            IsPrinterEditMode = false;
            IsPrinterFormVisible = true;
        }

        private void CancelPrinterForm()
        {
            SelectedPrinter = null;
            PrinterFormName = string.Empty;
            PrinterFormRealName = string.Empty;
            IsPrinterFormVisible = false;
        }

        private void EditPrinter(PrinterDto? printer)
        {
            if (printer == null) return;
            SelectedPrinter = printer;
            PrinterFormName = printer.Name;
            PrinterFormRealName = printer.PrinterName;
            PrinterFormType = printer.PrinterType;
            PrinterFormStationId = printer.PrintStationId;
            IsPrinterEditMode = true;
            IsPrinterFormVisible = true;
        }

        private bool CanSavePrinter() => !IsBusy &&
                                         !string.IsNullOrWhiteSpace(PrinterFormName) &&
                                         !string.IsNullOrWhiteSpace(PrinterFormRealName) &&
                                         PrinterFormStationId > 0;

        private async Task SavePrinterAsync()
        {
            if (!CanSavePrinter()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsPrinterEditMode && SelectedPrinter != null)
                {
                    var dto = new PrinterUpdateDto
                    {
                        Id = SelectedPrinter.Id,
                        Name = PrinterFormName,
                        PrinterName = PrinterFormRealName,
                        PrinterType = PrinterFormType,
                        PrintStationId = PrinterFormStationId
                    };
                    var result = await _printerApiService.UpdateAsync(SelectedPrinter.Id, dto);
                    if (result.IsSuccess)
                    {
                        var p = Printers.FirstOrDefault(pr => pr.Id == SelectedPrinter.Id);
                        if (p != null)
                        {
                            p.Name = PrinterFormName;
                            p.PrinterName = PrinterFormRealName;
                            p.PrinterType = PrinterFormType;
                            p.PrintStationId = PrinterFormStationId;
                            p.PrintStationName = PrintStations.FirstOrDefault(st => st.Id == PrinterFormStationId)?.Name ?? string.Empty;
                        }
                        CancelPrinterForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل تعديل الطابعة.";
                }
                else
                {
                    var dto = new PrinterCreateDto
                    {
                        Name = PrinterFormName,
                        PrinterName = PrinterFormRealName,
                        PrinterType = PrinterFormType,
                        PrintStationId = PrinterFormStationId
                    };
                    var result = await _printerApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        Printers.Add(result.Data);
                        CancelPrinterForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل إضافة الطابعة.";
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

        private async Task DeletePrinterAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف هذه الطابعة؟",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _printerApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var p = Printers.FirstOrDefault(pr => pr.Id == id);
                    if (p != null) Printers.Remove(p);
                    CancelPrinterForm();
                }
                else ErrorMessage = result.ErrorMessage ?? "فشل حذف الطابعة.";
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

        // --- Cashbox Logical Actions ---
        private void ShowAddCashboxForm()
        {
            SelectedCashbox = null;
            CashboxFormName = string.Empty;
            CashboxFormDescription = string.Empty;
            CashboxFormInitialBalance = 0;
            CashboxFormIsActive = true;
            IsCashboxEditMode = false;
            IsCashboxFormVisible = true;
        }

        private void CancelCashboxForm()
        {
            SelectedCashbox = null;
            CashboxFormName = string.Empty;
            CashboxFormDescription = string.Empty;
            IsCashboxFormVisible = false;
        }

        private void EditCashbox(CashboxDto? cashbox)
        {
            if (cashbox == null) return;
            SelectedCashbox = cashbox;
            CashboxFormName = cashbox.Name;
            CashboxFormDescription = cashbox.Description;
            CashboxFormInitialBalance = cashbox.InitialBalance;
            CashboxFormIsActive = cashbox.IsActive;
            IsCashboxEditMode = true;
            IsCashboxFormVisible = true;
        }

        private bool CanSaveCashbox() => !IsBusy && !string.IsNullOrWhiteSpace(CashboxFormName);

        private async Task SaveCashboxAsync()
        {
            if (!CanSaveCashbox()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsCashboxEditMode && SelectedCashbox != null)
                {
                    var dto = new CashboxUpdateDto
                    {
                        Id = SelectedCashbox.Id,
                        Name = CashboxFormName,
                        Description = CashboxFormDescription,
                        IsActive = CashboxFormIsActive
                    };
                    var result = await _cashboxApiService.UpdateAsync(SelectedCashbox.Id, dto);
                    if (result.IsSuccess)
                    {
                        var c = Cashboxes.FirstOrDefault(cb => cb.Id == SelectedCashbox.Id);
                        if (c != null)
                        {
                            c.Name = CashboxFormName;
                            c.Description = CashboxFormDescription;
                            c.IsActive = CashboxFormIsActive;
                        }
                        CancelCashboxForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل تعديل الخزينة.";
                }
                else
                {
                    var dto = new CashboxCreateDto
                    {
                        Name = CashboxFormName,
                        Description = CashboxFormDescription,
                        InitialBalance = CashboxFormInitialBalance,
                        IsActive = CashboxFormIsActive
                    };
                    var result = await _cashboxApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        Cashboxes.Add(result.Data);
                        CancelCashboxForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل إضافة الخزينة.";
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

        private async Task DeleteCashboxAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف هذه الخزينة؟",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _cashboxApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var c = Cashboxes.FirstOrDefault(cb => cb.Id == id);
                    if (c != null) Cashboxes.Remove(c);
                    CancelCashboxForm();
                }
                else ErrorMessage = result.ErrorMessage ?? "فشل حذف الخزينة.";
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

        private void SaveDefaultCashbox()
        {
            try
            {
                AppSettings.Instance.DefaultCashboxId = SettingsDefaultCashboxId;
                AppSettings.Instance.Save();
                System.Windows.MessageBox.Show("تم حفظ الخزينة الافتراضية للجهاز بنجاح!", "حفظ الإعدادات", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ أثناء حفظ الخزينة الافتراضية: {ex.Message}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // --- Addon Logical Actions ---
        private void ShowAddAddonForm()
        {
            SelectedAddon = null;
            AddonFormName = string.Empty;
            IsAddonEditMode = false;
            IsAddonFormVisible = true;
        }

        private void CancelAddonForm()
        {
            SelectedAddon = null;
            AddonFormName = string.Empty;
            IsAddonFormVisible = false;
        }

        private void EditAddon(AddonDto? addon)
        {
            if (addon == null) return;
            SelectedAddon = addon;
            AddonFormName = addon.Name;
            IsAddonEditMode = true;
            IsAddonFormVisible = true;
        }

        private bool CanSaveAddon() => !IsBusy && !string.IsNullOrWhiteSpace(AddonFormName);

        private async Task SaveAddonAsync()
        {
            if (string.IsNullOrWhiteSpace(AddonFormName)) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsAddonEditMode && SelectedAddon != null)
                {
                    var dto = new AddonUpdateDto { Id = SelectedAddon.Id, Name = AddonFormName };
                    var result = await _addonApiService.UpdateAsync(SelectedAddon.Id, dto);
                    if (result.IsSuccess)
                    {
                        var a = Addons.FirstOrDefault(ad => ad.Id == SelectedAddon.Id);
                        if (a != null) a.Name = AddonFormName;
                        CancelAddonForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل تعديل الإضافة.";
                }
                else
                {
                    var dto = new AddonCreateDto { Name = AddonFormName };
                    var result = await _addonApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        Addons.Add(result.Data);
                        CancelAddonForm();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل إضافة الإضافة.";
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

        private async Task DeleteAddonAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف هذا الخيار الإضافي؟",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _addonApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var a = Addons.FirstOrDefault(ad => ad.Id == id);
                    if (a != null) Addons.Remove(a);
                    CancelAddonForm();
                }
                else ErrorMessage = result.ErrorMessage ?? "فشل حذف الإضافة.";
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

        // --- Payment Method Logical Actions ---
        private void ShowAddPaymentMethodForm()
        {
            SelectedPaymentMethod = null;
            PaymentMethodFormName = string.Empty;
            PaymentMethodFormIsTaxFree = false;
            PaymentMethodFormLogoUrl = null;
            IsPaymentMethodEditMode = false;
            IsPaymentMethodFormVisible = true;
        }

        private void CancelPaymentMethodForm()
        {
            SelectedPaymentMethod = null;
            PaymentMethodFormName = string.Empty;
            PaymentMethodFormIsTaxFree = false;
            PaymentMethodFormLogoUrl = null;
            IsPaymentMethodFormVisible = false;
        }

        private void EditPaymentMethod(PaymentMethodDto? pm)
        {
            if (pm == null) return;
            SelectedPaymentMethod = pm;
            PaymentMethodFormName = pm.Name;
            PaymentMethodFormIsTaxFree = pm.IsTaxFree;
            PaymentMethodFormLogoUrl = pm.LogoUrl;
            IsPaymentMethodEditMode = true;
            IsPaymentMethodFormVisible = true;
        }

        private bool CanSavePaymentMethod() => !IsBusy && !string.IsNullOrWhiteSpace(PaymentMethodFormName);

        private async Task SavePaymentMethodAsync()
        {
            if (!CanSavePaymentMethod()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsPaymentMethodEditMode && SelectedPaymentMethod != null)
                {
                    var dto = new PaymentMethodUpdateDto
                    {
                        Id = SelectedPaymentMethod.Id,
                        Name = PaymentMethodFormName,
                        IsTaxFree = PaymentMethodFormIsTaxFree,
                        LogoUrl = PaymentMethodFormLogoUrl
                    };
                    var result = await _paymentMethodApiService.UpdateAsync(SelectedPaymentMethod.Id, dto);
                    if (result.IsSuccess)
                    {
                        var pm = PaymentMethods.FirstOrDefault(p => p.Id == SelectedPaymentMethod.Id);
                        if (pm != null)
                        {
                            pm.Name = PaymentMethodFormName;
                            pm.IsTaxFree = PaymentMethodFormIsTaxFree;
                            pm.LogoUrl = PaymentMethodFormLogoUrl;
                        }
                        CancelPaymentMethodForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل تعديل طريقة الدفع.";
                }
                else
                {
                    var dto = new PaymentMethodCreateDto
                    {
                        Name = PaymentMethodFormName,
                        IsTaxFree = PaymentMethodFormIsTaxFree,
                        LogoUrl = PaymentMethodFormLogoUrl
                    };
                    var result = await _paymentMethodApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        PaymentMethods.Add(result.Data);
                        CancelPaymentMethodForm();
                        _ = LoadDataAsync();
                    }
                    else ErrorMessage = result.ErrorMessage ?? "فشل إضافة طريقة الدفع.";
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

        private async Task DeletePaymentMethodAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف طريقة الدفع هذه؟",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _paymentMethodApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var pm = PaymentMethods.FirstOrDefault(p => p.Id == id);
                    if (pm != null) PaymentMethods.Remove(pm);
                    CancelPaymentMethodForm();
                }
                else ErrorMessage = result.ErrorMessage ?? "فشل حذف طريقة الدفع.";
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

        private async Task SelectPaymentMethodLogoAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ClearErrors();
                IsBusy = true;
                try
                {
                    var uploadResult = await _paymentMethodApiService.UploadLogoAsync(openFileDialog.FileName);
                    if (uploadResult.IsSuccess && uploadResult.Data != null)
                    {
                        PaymentMethodFormLogoUrl = uploadResult.Data;
                    }
                    else
                    {
                        ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع الصورة.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"خطأ في رفع الملف: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        // --- Restaurant Logical Actions ---
        private bool CanSaveRestaurant() => !IsBusy && !string.IsNullOrWhiteSpace(RestaurantFormName);

        private async Task SaveRestaurantAsync()
        {
            if (CurrentRestaurant == null) return;
            if (!CanSaveRestaurant()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var dto = new RestaurantUpdateDto
                {
                    Id = CurrentRestaurant.Id,
                    Name = RestaurantFormName,
                    Address = RestaurantFormAddress,
                    PhoneNumber = RestaurantFormPhoneNumber,
                    TaxNumber = RestaurantFormTaxNumber,
                    LogoUrl = RestaurantFormLogoUrl
                };

                var result = await _restaurantApiService.UpdateAsync(CurrentRestaurant.Id, dto);
                if (result.IsSuccess)
                {
                    System.Windows.MessageBox.Show("تم حفظ بيانات المطعم بنجاح!", "حفظ البيانات", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    _ = LoadDataAsync();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل حفظ بيانات المطعم.";
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

        private async Task CreateRestaurantAsync()
        {
            if (!CanSaveRestaurant()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var dto = new RestaurantCreateDto
                {
                    Name = RestaurantFormName,
                    Address = RestaurantFormAddress,
                    PhoneNumber = RestaurantFormPhoneNumber,
                    TaxNumber = RestaurantFormTaxNumber,
                    LogoUrl = RestaurantFormLogoUrl
                };

                var result = await _restaurantApiService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    CurrentRestaurant = result.Data;
                    
                    // Update current user's session locally so it has the RestaurantId
                    var user = SessionManager.Instance.CurrentUser;
                    if (user != null)
                    {
                        user.RestaurantId = result.Data.Id;
                    }
                    
                    System.Windows.MessageBox.Show("تم إنشاء وتعيين المطعم بنجاح!", "إنشاء المطعم", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    _ = LoadDataAsync();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل إنشاء المطعم.";
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

        private async Task SelectRestaurantLogoAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ClearErrors();
                IsBusy = true;
                try
                {
                    var uploadResult = await _restaurantApiService.UploadLogoAsync(openFileDialog.FileName);
                    if (uploadResult.IsSuccess && uploadResult.Data != null)
                    {
                        RestaurantFormLogoUrl = uploadResult.Data;
                    }
                    else
                    {
                        ErrorMessage = uploadResult.ErrorMessage ?? "فشل رفع الشعار.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"خطأ في رفع الملف: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        // --- User Command Actions ---

        private void CloseAllForms()
        {
            IsStationFormVisible = false;
            IsPrinterFormVisible = false;
            IsCashboxFormVisible = false;
            IsAddonFormVisible = false;
            IsPaymentMethodFormVisible = false;
            IsUserFormVisible = false;
            IsPasswordFormVisible = false;
        }

        private void ShowAddUserForm()
        {
            ClearErrors();
            CloseAllForms();
            IsUserEditMode = false;
            UserFormUsername = string.Empty;
            UserFormEmail = string.Empty;
            UserFormPhoneNumber = string.Empty;
            UserFormPassword = string.Empty;
            UserFormIsAdmin = false;
            UserFormIsCashier = true; // Cashier by default
            UserFormPermDashboard = false;
            UserFormPermNewOrder = true;
            UserFormPermOrders = true;
            UserFormPermProducts = false;
            UserFormPermCategories = false;
            UserFormPermReports = false;
            UserFormPermSettings = false;
            UserFormPermTreasury = false;
            UserFormPermPOSApplyDiscount = false;
            UserFormPermPOSVoidItem = true;
            UserFormPermOrdersVoidOrder = false;
            UserFormPermShiftViewTotals = false; // Blind close by default for new cashiers
            UserFormPermProductsManage = false;
            UserFormPermCategoriesManage = false;
            SelectedUser = null;
            IsUserFormVisible = true;
        }

        private void CancelUserForm()
        {
            IsUserFormVisible = false;
            ClearErrors();
        }

        private void EditUser(UserDto user)
        {
            ClearErrors();
            CloseAllForms();
            IsUserEditMode = true;
            SelectedUser = user;
            UserFormUsername = user.UserName ?? string.Empty;
            UserFormEmail = user.Email ?? string.Empty;
            UserFormPhoneNumber = user.PhoneNumber ?? string.Empty;
            UserFormPassword = string.Empty; // Not used in edit mode
            UserFormIsAdmin = user.Roles != null && user.Roles.Contains("Admin");
            UserFormIsCashier = user.Roles != null && user.Roles.Contains("Cashier");
            UserFormPermDashboard = user.Permissions != null && user.Permissions.Contains("Permission.Dashboard");
            UserFormPermNewOrder = user.Permissions != null && user.Permissions.Contains("Permission.NewOrder");
            UserFormPermOrders = user.Permissions != null && user.Permissions.Contains("Permission.Orders");
            UserFormPermProducts = user.Permissions != null && user.Permissions.Contains("Permission.Products");
            UserFormPermCategories = user.Permissions != null && user.Permissions.Contains("Permission.Categories");
            UserFormPermReports = user.Permissions != null && user.Permissions.Contains("Permission.Reports");
            UserFormPermSettings = user.Permissions != null && user.Permissions.Contains("Permission.Settings");
            UserFormPermTreasury = user.Permissions != null && user.Permissions.Contains("Permission.Treasury");
            UserFormPermPOSApplyDiscount = user.Permissions != null && user.Permissions.Contains("Permission.POS.ApplyDiscount");
            UserFormPermPOSVoidItem = user.Permissions != null && user.Permissions.Contains("Permission.POS.VoidItem");
            UserFormPermOrdersVoidOrder = user.Permissions != null && user.Permissions.Contains("Permission.Orders.VoidOrder");
            UserFormPermShiftViewTotals = user.Permissions != null && user.Permissions.Contains("Permission.Shift.ViewTotals");
            UserFormPermProductsManage = user.Permissions != null && user.Permissions.Contains("Permission.Products.Manage");
            UserFormPermCategoriesManage = user.Permissions != null && user.Permissions.Contains("Permission.Categories.Manage");
            IsUserFormVisible = true;
        }

        private bool CanSaveUser()
        {
            if (IsUserEditMode)
            {
                return !string.IsNullOrWhiteSpace(UserFormUsername) && !string.IsNullOrWhiteSpace(UserFormEmail);
            }
            return !string.IsNullOrWhiteSpace(UserFormUsername) && 
                   !string.IsNullOrWhiteSpace(UserFormEmail) && 
                   !string.IsNullOrWhiteSpace(UserFormPassword) && 
                   UserFormPassword.Length >= 8;
        }

        private async Task SaveUserAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var roles = new List<string>();
                if (UserFormIsAdmin) roles.Add("Admin");
                if (UserFormIsCashier) roles.Add("Cashier");

                var permissions = new List<string>();
                if (UserFormPermDashboard) permissions.Add("Permission.Dashboard");
                if (UserFormPermNewOrder) permissions.Add("Permission.NewOrder");
                if (UserFormPermOrders) permissions.Add("Permission.Orders");
                if (UserFormPermProducts) permissions.Add("Permission.Products");
                if (UserFormPermCategories) permissions.Add("Permission.Categories");
                if (UserFormPermReports) permissions.Add("Permission.Reports");
                if (UserFormPermSettings) permissions.Add("Permission.Settings");
                if (UserFormPermTreasury) permissions.Add("Permission.Treasury");
                if (UserFormPermPOSApplyDiscount) permissions.Add("Permission.POS.ApplyDiscount");
                if (UserFormPermPOSVoidItem) permissions.Add("Permission.POS.VoidItem");
                if (UserFormPermOrdersVoidOrder) permissions.Add("Permission.Orders.VoidOrder");
                if (UserFormPermShiftViewTotals) permissions.Add("Permission.Shift.ViewTotals");
                if (UserFormPermProductsManage) permissions.Add("Permission.Products.Manage");
                if (UserFormPermCategoriesManage) permissions.Add("Permission.Categories.Manage");

                if (IsUserEditMode && SelectedUser != null)
                {
                    // Update user properties
                    var updateDto = new UserUpdateDto
                    {
                        UserName = UserFormUsername,
                        Email = UserFormEmail,
                        PhoneNumber = string.IsNullOrWhiteSpace(UserFormPhoneNumber) ? null : UserFormPhoneNumber,
                        Permissions = permissions
                    };

                    var updateResult = await _userApiService.UpdateUserAsync(SelectedUser.Id, updateDto);
                    if (!updateResult.IsSuccess)
                    {
                        ErrorMessage = updateResult.ErrorMessage ?? "فشل تعديل بيانات المستخدم.";
                        return;
                    }

                    // Update user roles
                    var rolesResult = await _userApiService.UpdateUserRolesAsync(SelectedUser.Id, roles);
                    if (!rolesResult.IsSuccess)
                    {
                        ErrorMessage = rolesResult.ErrorMessage ?? "فشل تعديل أدوار المستخدم.";
                        return;
                    }
                }
                else
                {
                    // Create user via register
                    var registerDto = new RegisterRequestDto
                    {
                        UserName = UserFormUsername,
                        Email = UserFormEmail,
                        Password = UserFormPassword,
                        PhoneNumber = string.IsNullOrWhiteSpace(UserFormPhoneNumber) ? null : UserFormPhoneNumber,
                        Permissions = permissions
                    };

                    var registerResult = await _userApiService.RegisterUserAsync(registerDto);
                    if (!registerResult.IsSuccess || registerResult.Data == null || !registerResult.Data.IsSuccess)
                    {
                        ErrorMessage = registerResult.ErrorMessage ?? registerResult.Data?.Message ?? "فشل إضافة مستخدم جديد.";
                        return;
                    }

                    var newUser = registerResult.Data.User;
                    if (newUser != null)
                    {
                        // Update role if roles are specified
                        var rolesResult = await _userApiService.UpdateUserRolesAsync(newUser.Id, roles);
                        if (!rolesResult.IsSuccess)
                        {
                            ErrorMessage = rolesResult.ErrorMessage ?? "تم إنشاء المستخدم ولكن فشل تعيين صلاحياته.";
                            return;
                        }
                    }
                }

                IsUserFormVisible = false;
                await LoadDataAsync(); // Refresh list
            }
            catch (Exception ex)
            {
                ErrorMessage = $"حدث خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            // Prevent deleting current user
            if (SessionManager.Instance.CurrentUser?.Id == userId)
            {
                System.Windows.MessageBox.Show("لا يمكنك حذف حسابك الحالي الذي تقوم بتسجيل الدخول به!", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var confirm = System.Windows.MessageBox.Show("هل أنت متأكد من رغبتك في حذف هذا المستخدم نهائياً؟", "تأكيد الحذف", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (confirm == System.Windows.MessageBoxResult.Yes)
            {
                ClearErrors();
                IsBusy = true;
                try
                {
                    var result = await _userApiService.DeleteUserAsync(userId);
                    if (result.IsSuccess)
                    {
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل حذف المستخدم.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"حدث خطأ: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void ChangeUserPassword(UserDto user)
        {
            ClearErrors();
            CloseAllForms();
            SelectedUser = user;
            PasswordFormNewPassword = string.Empty;
            IsPasswordFormVisible = true;
        }

        private bool CanSavePassword()
        {
            return !string.IsNullOrWhiteSpace(PasswordFormNewPassword) && PasswordFormNewPassword.Length >= 8;
        }

        private async Task SavePasswordAsync()
        {
            if (SelectedUser == null) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var resetDto = new UserResetPasswordDto
                {
                    NewPassword = PasswordFormNewPassword
                };

                var result = await _userApiService.ResetPasswordAsync(SelectedUser.Id, resetDto);
                if (result.IsSuccess)
                {
                    System.Windows.MessageBox.Show("تم تغيير كلمة المرور للمستخدم بنجاح!", "تغيير كلمة المرور", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    IsPasswordFormVisible = false;
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تغيير كلمة المرور.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"حدث خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CancelPasswordForm()
        {
            IsPasswordFormVisible = false;
            ClearErrors();
        }
    }
}
