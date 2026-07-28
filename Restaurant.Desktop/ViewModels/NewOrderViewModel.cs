using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.ViewModels
{
    public class CartItemModel : ObservableObject
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal UnitCostPrice { get; set; }
        public string? ImageUrl { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        private string _notes = "لا يوجد ملاحظات";
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class NewOrderViewModel : BaseViewModel
    {
        private readonly IProductApiService _productApiService;
        private readonly ICategoryApiService _categoryApiService;
        private readonly IOrderApiService _orderApiService;
        private readonly ICashDrawerEntryApiService _cashDrawerEntryApiService;
        private readonly IPaymentMethodApiService _paymentMethodApiService;
        private readonly IAddonApiService _addonApiService;
        private readonly IWpfPrintingService _wpfPrintingService;
        private readonly IToastService _toastService;

        private List<AddonDto> _allAddons = new();

        // Categories list
        private ObservableCollection<CategoryDto> _categories = new();
        public ObservableCollection<CategoryDto> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // All Products
        private ObservableCollection<ProductDto> _allProducts = new();
        public ObservableCollection<ProductDto> AllProducts
        {
            get => _allProducts;
            set => SetProperty(ref _allProducts, value);
        }

        // Filtered products to display
        private ObservableCollection<ProductDto> _filteredProducts = new();
        public ObservableCollection<ProductDto> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        // Category filter
        private CategoryDto? _selectedCategory;
        public CategoryDto? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilter();
                }
            }
        }

        // Cart items
        public ObservableCollection<CartItemModel> CartItems { get; } = new();

        // Payment Methods list
        private ObservableCollection<PaymentMethodDto> _paymentMethods = new();
        public ObservableCollection<PaymentMethodDto> PaymentMethods
        {
            get => _paymentMethods;
            set => SetProperty(ref _paymentMethods, value);
        }

        private PaymentMethodDto? _selectedPaymentMethod;
        public PaymentMethodDto? SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (SetProperty(ref _selectedPaymentMethod, value))
                {
                    ConfirmOrderCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private OrderType _selectedOrderType = OrderType.Indoor;
        public OrderType SelectedOrderType
        {
            get => _selectedOrderType;
            set => SetProperty(ref _selectedOrderType, value);
        }

        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public bool CanApplyDiscount => HasPermission("Permission.POS.ApplyDiscount");
        public bool CanVoidItem => HasPermission("Permission.POS.VoidItem");

        private bool HasPermission(string permissionName)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            if (user.Roles != null && user.Roles.Contains("Admin")) return true;
            return user.Permissions != null && user.Permissions.Contains(permissionName);
        }

        private string _notes = "لا يوجد ملاحظات";
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private int _orderNumber;
        public int OrderNumber
        {
            get => _orderNumber;
            set => SetProperty(ref _orderNumber, value);
        }

        // Calculated totals
        public decimal SubTotal => CartItems.Sum(item => item.LineTotal);
        public decimal Total => Math.Max(0, SubTotal - Discount);
        public bool IsCartEmpty => !CartItems.Any();
        public bool IsCatalogEmpty => FilteredProducts == null || !FilteredProducts.Any();

        public AsyncRelayCommand LoadDataCommand { get; }
        public AsyncRelayCommand ConfirmOrderCommand { get; }
        public AsyncRelayCommand<PaymentMethodDto> ConfirmOrderWithPaymentCommand { get; }
        public RelayCommand<ProductDto> AddToCartCommand { get; }
        public RelayCommand<CartItemModel> IncreaseQuantityCommand { get; }
        public RelayCommand<CartItemModel> DecreaseQuantityCommand { get; }
        public RelayCommand<CartItemModel> RemoveFromCartCommand { get; }
        public RelayCommand<CartItemModel> EditCartItemCommand { get; }
        public RelayCommand ResetCartCommand { get; }

        public NewOrderViewModel(
            IProductApiService productApiService,
            ICategoryApiService categoryApiService,
            IOrderApiService orderApiService,
            ICashDrawerEntryApiService cashDrawerEntryApiService,
            IPaymentMethodApiService paymentMethodApiService,
            IAddonApiService addonApiService,
            IWpfPrintingService wpfPrintingService,
            IToastService toastService)
        {
            _productApiService = productApiService;
            _categoryApiService = categoryApiService;
            _orderApiService = orderApiService;
            _cashDrawerEntryApiService = cashDrawerEntryApiService;
            _paymentMethodApiService = paymentMethodApiService;
            _addonApiService = addonApiService;
            _wpfPrintingService = wpfPrintingService;
            _toastService = toastService;

            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            ConfirmOrderCommand = new AsyncRelayCommand(ConfirmOrderAsync, CanConfirmOrder);
            ConfirmOrderWithPaymentCommand = new AsyncRelayCommand<PaymentMethodDto>(ConfirmOrderWithPaymentAsync, CanConfirmOrderWithPayment);
            AddToCartCommand = new RelayCommand<ProductDto>(AddToCart);
            IncreaseQuantityCommand = new RelayCommand<CartItemModel>(IncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItemModel>(DecreaseQuantity);
            RemoveFromCartCommand = new RelayCommand<CartItemModel>(RemoveFromCart);
            EditCartItemCommand = new RelayCommand<CartItemModel>(EditCartItem);
            ResetCartCommand = new RelayCommand(ResetCart);

            CartItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(SubTotal));
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(IsCartEmpty));
                ConfirmOrderCommand.RaiseCanExecuteChanged();
                ConfirmOrderWithPaymentCommand.RaiseCanExecuteChanged();
            };

            GenerateOrderNumber();
            _ = LoadDataAsync();
        }

        private void GenerateOrderNumber()
        {
            // Simple random/timestamp order number generator
            OrderNumber = new Random().Next(1000, 99999);
        }

        private bool CanConfirmOrder()
        {
            return !IsBusy && CartItems.Any() && SelectedPaymentMethod != null;
        }

        private async Task LoadDataAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var catsResult = await _categoryApiService.GetAllAsync();
                if (catsResult.IsSuccess && catsResult.Data != null)
                {
                    Categories = new ObservableCollection<CategoryDto>(catsResult.Data);
                }

                var prodsResult = await _productApiService.GetAllAsync();
                if (prodsResult.IsSuccess && prodsResult.Data != null)
                {
                    AllProducts = new ObservableCollection<ProductDto>(prodsResult.Data);
                    ApplyFilter();
                }

                var pmResult = await _paymentMethodApiService.GetAllAsync();
                if (pmResult.IsSuccess && pmResult.Data != null)
                {
                    PaymentMethods = new ObservableCollection<PaymentMethodDto>(pmResult.Data);
                    if (PaymentMethods.Any())
                    {
                        SelectedPaymentMethod = PaymentMethods.First();
                    }
                }

                var addonsResult = await _addonApiService.GetAllAsync();
                if (addonsResult.IsSuccess && addonsResult.Data != null)
                {
                    _allAddons = addonsResult.Data;
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

        private void ApplyFilter()
        {
            if (SelectedCategory == null)
            {
                FilteredProducts = new ObservableCollection<ProductDto>(AllProducts);
            }
            else
            {
                var filtered = AllProducts.Where(p => p.CategoryId == SelectedCategory.Id).ToList();
                FilteredProducts = new ObservableCollection<ProductDto>(filtered);
            }
            OnPropertyChanged(nameof(IsCatalogEmpty));
        }

        private void AddToCart(ProductDto? product)
        {
            if (product == null) return;

            var existing = CartItems.FirstOrDefault(item => item.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
                ConfirmOrderWithPaymentCommand.RaiseCanExecuteChanged();
            }
            else
            {
                CartItems.Add(new CartItemModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.SalePrice,
                    UnitCostPrice = product.CostPrice,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1,
                    Notes = "لا يوجد ملاحظات"
                });
            }
        }

        private void IncreaseQuantity(CartItemModel? item)
        {
            if (item == null) return;
            item.Quantity++;
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(Total));
        }

        private void DecreaseQuantity(CartItemModel? item)
        {
            if (item == null) return;
            item.Quantity--;
            if (item.Quantity <= 0)
            {
                CartItems.Remove(item);
            }
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(Total));
        }

        private void RemoveFromCart(CartItemModel? item)
        {
            if (item == null) return;
            CartItems.Remove(item);
        }

        private void ResetCart()
        {
            if (CartItems.Any())
            {
                var owner = System.Windows.Application.Current.MainWindow;
                var confirmed = Restaurant.Desktop.Controls.ConfirmWindow.Show(
                    owner,
                    "تأكيد مسح السلة",
                    "هل أنت متأكد من رغبتك في مسح كافة العناصر المضافة في السلة وتصفير الطلب؟",
                    "نعم، أفرغ السلة",
                    "danger");

                if (!confirmed) return;
            }

            CartItems.Clear();
            Discount = 0;
            Notes = "لا يوجد ملاحظات";
            GenerateOrderNumber();
        }

        private async Task ConfirmOrderAsync()
        {
            if (!CartItems.Any() || SelectedPaymentMethod == null) return;

            var defaultCashboxId = AppSettings.Instance.DefaultCashboxId;
            if (!defaultCashboxId.HasValue || defaultCashboxId.Value <= 0)
            {
                _toastService.ShowWarning("لم يتم ضبط خزينة افتراضية لهذا الجهاز. يرجى مراجعة إعدادات النظام وتحديد الخزينة الافتراضية أولاً.");
                return;
            }

            ClearErrors();
            IsBusy = true;
            try
            {
                // Create DTO
                var orderItems = CartItems.Select(item => new OrderItemCreateDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitSalePrice = item.UnitPrice,
                    UnitCostPrice = item.UnitCostPrice,
                    UnitDiscount = 0,
                    Notes = item.Notes
                }).ToList();

                var currentUserId = SessionManager.Instance.CurrentUser?.Id ?? "1"; // Default fallbacks if needed

                var orderCreateDto = new OrderCreateDto
                {
                    OrderNumber = OrderNumber,
                    Discount = Discount,
                    OrderStatus = AppSettings.Instance.DefaultOrderStatus,
                    OrderType = SelectedOrderType,
                    Notes = Notes,
                    UserId = currentUserId,
                    OrderItems = orderItems
                };

                var result = await _orderApiService.CreateAsync(orderCreateDto);
                if (result.IsSuccess && result.Data != null)
                {
                    // Create cash drawer entry for the payment
                    var cashDrawerEntryCreateDto = new CashDrawerEntryCreateDto
                    {
                        CashboxId = defaultCashboxId.Value,
                        Amount = result.Data.Total,
                        EntryType = CashDrawerEntryType.SalePayment,
                        Notes = $"سداد الطلب رقم {result.Data.OrderNumber}",
                        PaymentMethodId = SelectedPaymentMethod.Id,
                        OrderId = result.Data.Id,
                        UserId = currentUserId
                    };
                    
                    var entryResult = await _cashDrawerEntryApiService.CreateAsync(cashDrawerEntryCreateDto);
                    if (!entryResult.IsSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to record cash drawer entry: {entryResult.ErrorMessage}");
                    }

                    // Print the order in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _wpfPrintingService.PrintOrderAsync(result.Data);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Printing error: {ex.Message}");
                        }
                    });

                    _toastService.ShowSuccess($"تم حفظ الطلب بنجاح! رقم الطلب: {OrderNumber}");
                    ResetCart();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل إرسال الطلب للخادم.";
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

        private bool CanConfirmOrderWithPayment(PaymentMethodDto? method)
        {
            return !IsBusy && CartItems.Any();
        }

        private async Task ConfirmOrderWithPaymentAsync(PaymentMethodDto? method)
        {
            if (method == null) return;
            SelectedPaymentMethod = method;
            await ConfirmOrderAsync();
        }

        private void EditCartItem(CartItemModel? item)
        {
            if (item == null) return;

            var window = new Views.ExcludeAddonsWindow(_allAddons, item.Notes)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                item.Notes = window.ResultNotes;
                OnPropertyChanged(nameof(SubTotal));
                OnPropertyChanged(nameof(Total));
            }
        }
    }
}
