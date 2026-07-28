using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;
using Microsoft.Win32;

namespace Restaurant.Desktop.ViewModels
{
    public class SortOption
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class ProductsViewModel : BaseViewModel
    {
        private readonly IProductApiService _productApiService;
        private readonly ICategoryApiService _categoryApiService;

        private ObservableCollection<ProductDto> _products = new();
        public ObservableCollection<ProductDto> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private ObservableCollection<CategoryDto> _categories = new();
        public ObservableCollection<CategoryDto> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private ObservableCollection<CategoryDto> _filterCategories = new();
        public ObservableCollection<CategoryDto> FilterCategories
        {
            get => _filterCategories;
            set => SetProperty(ref _filterCategories, value);
        }

        private string _viewMode = "List";
        public string ViewMode
        {
            get => _viewMode;
            set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnPropertyChanged(nameof(IsListView));
                    OnPropertyChanged(nameof(IsCardsView));
                }
            }
        }

        public bool IsListView
        {
            get => ViewMode == "List";
            set { if (value) ViewMode = "List"; }
        }

        public bool IsCardsView
        {
            get => ViewMode == "Cards";
            set { if (value) ViewMode = "Cards"; }
        }

        public ObservableCollection<SortOption> SortOptions { get; } = new()
        {
            new SortOption { Key = "Name", DisplayName = "الاسم (أ - ي)" },
            new SortOption { Key = "PriceAsc", DisplayName = "سعر البيع (من الأقل للأعلى)" },
            new SortOption { Key = "PriceDesc", DisplayName = "سعر البيع (من الأعلى للأقل)" },
            new SortOption { Key = "CostAsc", DisplayName = "سعر التكلفة (من الأقل للأعلى)" },
            new SortOption { Key = "CostDesc", DisplayName = "سعر التكلفة (من الأعلى للأقل)" },
            new SortOption { Key = "ProfitAsc", DisplayName = "الربح (من الأقل للأعلى)" },
            new SortOption { Key = "ProfitDesc", DisplayName = "الربح (من الأعلى للأقل)" }
        };

        private string _selectedSortOption = "Name";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ApplyFilter();
                }
            }
        }

        private ObservableCollection<ProductDto> _filteredProducts = new();
        public ObservableCollection<ProductDto> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        public bool CanManageProducts => HasPermission("Permission.Products.Manage");

        private bool HasPermission(string permissionName)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            if (user.Roles != null && user.Roles.Contains("Admin")) return true;
            return user.Permissions != null && user.Permissions.Contains(permissionName);
        }

        private ProductDto? _selectedProduct;
        public ProductDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    if (value != null)
                    {
                        FormName = value.Name;
                        FormBarCode = value.BarCode ?? string.Empty;
                        FormDescription = value.Description ?? string.Empty;
                        FormCostPrice = value.CostPrice;
                        FormSalePrice = value.SalePrice;
                        FormImageUrl = value.ImageUrl ?? string.Empty;
                        FormCategoryId = value.CategoryId;
                        IsEditMode = true;
                        IsFormVisible = true;
                    }
                    else
                    {
                        CancelForm();
                    }
                }
            }
        }

        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetProperty(ref _isFormVisible, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Form Fields
        private string _formName = string.Empty;
        public string FormName
        {
            get => _formName;
            set
            {
                if (SetProperty(ref _formName, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _formBarCode = string.Empty;
        public string FormBarCode
        {
            get => _formBarCode;
            set => SetProperty(ref _formBarCode, value);
        }

        private string _formDescription = string.Empty;
        public string FormDescription
        {
            get => _formDescription;
            set => SetProperty(ref _formDescription, value);
        }

        private decimal _formCostPrice;
        public decimal FormCostPrice
        {
            get => _formCostPrice;
            set => SetProperty(ref _formCostPrice, value);
        }

        private decimal _formSalePrice;
        public decimal FormSalePrice
        {
            get => _formSalePrice;
            set => SetProperty(ref _formSalePrice, value);
        }

        private string _formImageUrl = string.Empty;
        public string FormImageUrl
        {
            get => _formImageUrl;
            set
            {
                if (SetProperty(ref _formImageUrl, value))
                {
                    OnPropertyChanged(nameof(ProductFormFullImageUrl));
                }
            }
        }

        public string ProductFormFullImageUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FormImageUrl)) return string.Empty;
                if (FormImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return FormImageUrl;
                return $"{AppSettings.Instance.ApiBaseUrl.TrimEnd('/')}/{FormImageUrl.TrimStart('/')}";
            }
        }

        private int _formCategoryId;
        public int FormCategoryId
        {
            get => _formCategoryId;
            set => SetProperty(ref _formCategoryId, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        private CategoryDto? _selectedCategoryFilter;
        public CategoryDto? SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedCategoryFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public AsyncRelayCommand LoadProductsCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand<int> DeleteCommand { get; }
        public RelayCommand ShowAddFormCommand { get; }
        public RelayCommand CancelFormCommand { get; }
        public RelayCommand<ProductDto> EditCommand { get; }
        public AsyncRelayCommand SelectImageCommand { get; }

        public ProductsViewModel(IProductApiService productApiService, ICategoryApiService categoryApiService)
        {
            _productApiService = productApiService;
            _categoryApiService = categoryApiService;

            LoadProductsCommand = new AsyncRelayCommand(LoadDataAsync);
            SaveCommand = new AsyncRelayCommand(SaveProductAsync, CanSave);
            DeleteCommand = new AsyncRelayCommand<int>(DeleteProductAsync);
            ShowAddFormCommand = new RelayCommand(ShowAddForm);
            CancelFormCommand = new RelayCommand(CancelForm);
            EditCommand = new RelayCommand<ProductDto>(prod => SelectedProduct = prod);
            SelectImageCommand = new AsyncRelayCommand(SelectImageAsync);

            _ = LoadDataAsync();
        }

        private async Task SelectImageAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "اختر صورة المنتج"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ClearErrors();
                IsBusy = true;
                try
                {
                    var result = await _productApiService.UploadImageAsync(openFileDialog.FileName);
                    if (result.IsSuccess && result.Data != null)
                    {
                        FormImageUrl = result.Data;
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل رفع الصورة على الخادم.";
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

        private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(FormName) && FormCategoryId > 0 && FormSalePrice > 0;

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

                    var filterList = new List<CategoryDto> { new CategoryDto { Id = 0, Name = "الكل" } };
                    filterList.AddRange(catsResult.Data);
                    FilterCategories = new ObservableCollection<CategoryDto>(filterList);
                    SelectedCategoryFilter = filterList.First();
                }

                var prodsResult = await _productApiService.GetAllAsync();
                if (prodsResult.IsSuccess && prodsResult.Data != null)
                {
                    Products = new ObservableCollection<ProductDto>(prodsResult.Data);
                    ApplyFilter();
                }
                else
                {
                    ErrorMessage = prodsResult.ErrorMessage ?? "فشل تحميل المنتجات.";
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
            var query = Products.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                         (p.BarCode != null && p.BarCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }

            if (SelectedCategoryFilter != null && SelectedCategoryFilter.Id > 0)
            {
                query = query.Where(p => p.CategoryId == SelectedCategoryFilter.Id);
            }

            switch (SelectedSortOption)
            {
                case "PriceAsc":
                    query = query.OrderBy(p => p.SalePrice);
                    break;
                case "PriceDesc":
                    query = query.OrderByDescending(p => p.SalePrice);
                    break;
                case "CostAsc":
                    query = query.OrderBy(p => p.CostPrice);
                    break;
                case "CostDesc":
                    query = query.OrderByDescending(p => p.CostPrice);
                    break;
                case "ProfitAsc":
                    query = query.OrderBy(p => p.Profit);
                    break;
                case "ProfitDesc":
                    query = query.OrderByDescending(p => p.Profit);
                    break;
                case "Name":
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            FilteredProducts = new ObservableCollection<ProductDto>(query.ToList());
        }

        private void ShowAddForm()
        {
            IsFormVisible = true;
            IsEditMode = false;
            SelectedProduct = null;
            FormName = string.Empty;
            FormBarCode = string.Empty;
            FormDescription = string.Empty;
            FormCostPrice = 0;
            FormSalePrice = 0;
            FormImageUrl = string.Empty;
            if (Categories.Any())
            {
                FormCategoryId = Categories.First().Id;
            }
        }

        private void CancelForm()
        {
            IsFormVisible = false;
            IsEditMode = false;
            FormName = string.Empty;
            FormBarCode = string.Empty;
            FormDescription = string.Empty;
            FormCostPrice = 0;
            FormSalePrice = 0;
            FormImageUrl = string.Empty;
        }

        private async Task SaveProductAsync()
        {
            if (string.IsNullOrWhiteSpace(FormName) || FormCategoryId == 0) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsEditMode && SelectedProduct != null)
                {
                    var dto = new ProductUpdateDto
                    {
                        Id = SelectedProduct.Id,
                        Name = FormName,
                        BarCode = string.IsNullOrWhiteSpace(FormBarCode) ? null : FormBarCode,
                        Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription,
                        CostPrice = FormCostPrice,
                        SalePrice = FormSalePrice,
                        ImageUrl = string.IsNullOrWhiteSpace(FormImageUrl) ? null : FormImageUrl,
                        CategoryId = FormCategoryId
                    };

                    var result = await _productApiService.UpdateAsync(SelectedProduct.Id, dto);
                    if (result.IsSuccess)
                    {
                        var prod = Products.FirstOrDefault(p => p.Id == SelectedProduct.Id);
                        if (prod != null)
                        {
                            prod.Name = FormName;
                            prod.BarCode = FormBarCode;
                            prod.Description = FormDescription;
                            prod.CostPrice = FormCostPrice;
                            prod.SalePrice = FormSalePrice;
                            prod.ImageUrl = FormImageUrl;
                            prod.CategoryId = FormCategoryId;
                            var catName = Categories.FirstOrDefault(c => c.Id == FormCategoryId)?.Name ?? string.Empty;
                            prod.CategoryName = catName;
                        }
                        CancelForm();
                        ApplyFilter();
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل تعديل المنتج.";
                    }
                }
                else
                {
                    var dto = new ProductCreateDto
                    {
                        Name = FormName,
                        BarCode = string.IsNullOrWhiteSpace(FormBarCode) ? null : FormBarCode,
                        Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription,
                        CostPrice = FormCostPrice,
                        SalePrice = FormSalePrice,
                        ImageUrl = string.IsNullOrWhiteSpace(FormImageUrl) ? null : FormImageUrl,
                        CategoryId = FormCategoryId
                    };

                    var result = await _productApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        Products.Add(result.Data);
                        CancelForm();
                        ApplyFilter();
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل إضافة المنتج.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ أثناء الحفظ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteProductAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف هذا المنتج؟",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _productApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var prod = Products.FirstOrDefault(p => p.Id == id);
                    if (prod != null)
                    {
                        Products.Remove(prod);
                    }
                    CancelForm();
                    ApplyFilter();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل حذف المنتج.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ أثناء الحذف: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
