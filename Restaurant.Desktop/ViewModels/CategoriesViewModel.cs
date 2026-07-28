using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.ViewModels
{
    public class CategoriesViewModel : BaseViewModel
    {
        private readonly ICategoryApiService _categoryApiService;
        private readonly IPrintStationApiService _printStationApiService;

        private ObservableCollection<CategoryDto> _categories = new();
        public ObservableCollection<CategoryDto> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private ObservableCollection<PrintStationDto> _printStations = new();
        public ObservableCollection<PrintStationDto> PrintStations
        {
            get => _printStations;
            set => SetProperty(ref _printStations, value);
        }

        private int? _selectedPrintStationId;
        public int? SelectedPrintStationId
        {
            get => _selectedPrintStationId;
            set => SetProperty(ref _selectedPrintStationId, value);
        }

        private int? _originalPrintStationId;

        private CategoryDto? _selectedCategory;
        public CategoryDto? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    if (value != null)
                    {
                        FormName = value.Name;
                        IsEditMode = true;
                        IsFormVisible = true;
                        _ = LoadLinkedPrintStationAsync(value.Id);
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

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplySearch();
                }
            }
        }

        public bool CanManageCategories => HasPermission("Permission.Categories.Manage");

        private bool HasPermission(string permissionName)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            if (user.Roles != null && user.Roles.Contains("Admin")) return true;
            return user.Permissions != null && user.Permissions.Contains(permissionName);
        }

        private ObservableCollection<CategoryDto> _filteredCategories = new();
        public ObservableCollection<CategoryDto> FilteredCategories
        {
            get => _filteredCategories;
            set => SetProperty(ref _filteredCategories, value);
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

        public AsyncRelayCommand LoadCategoriesCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand<int> DeleteCommand { get; }
        public RelayCommand ShowAddFormCommand { get; }
        public RelayCommand CancelFormCommand { get; }
        public RelayCommand<CategoryDto> EditCommand { get; }

        public CategoriesViewModel(ICategoryApiService categoryApiService, IPrintStationApiService printStationApiService)
        {
            _categoryApiService = categoryApiService;
            _printStationApiService = printStationApiService;

            LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
            SaveCommand = new AsyncRelayCommand(SaveCategoryAsync, CanSave);
            DeleteCommand = new AsyncRelayCommand<int>(DeleteCategoryAsync);
            ShowAddFormCommand = new RelayCommand(ShowAddForm);
            CancelFormCommand = new RelayCommand(CancelForm);
            EditCommand = new RelayCommand<CategoryDto>(cat => SelectedCategory = cat);

            _ = LoadCategoriesAsync();
        }

        private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(FormName);

        private async Task LoadCategoriesAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var stationsResult = await _printStationApiService.GetAllAsync();
                if (stationsResult.IsSuccess && stationsResult.Data != null)
                {
                    var list = new List<PrintStationDto>
                    {
                        new PrintStationDto { Id = 0, Name = "بدون محطة (طابعة فواتير فقط)" }
                    };
                    list.AddRange(stationsResult.Data);
                    PrintStations = new ObservableCollection<PrintStationDto>(list);
                }

                var result = await _categoryApiService.GetAllAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Categories = new ObservableCollection<CategoryDto>(result.Data);
                    ApplySearch();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تحميل التصنيفات.";
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

        private void ApplySearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCategories = new ObservableCollection<CategoryDto>(Categories);
            }
            else
            {
                var filtered = Categories.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
                FilteredCategories = new ObservableCollection<CategoryDto>(filtered);
            }
        }

        private void ShowAddForm()
        {
            IsFormVisible = true;
            IsEditMode = false;
            FormName = string.Empty;
            SelectedPrintStationId = 0;
            _originalPrintStationId = null;
            SelectedCategory = null;
        }

        private void CancelForm()
        {
            IsFormVisible = false;
            IsEditMode = false;
            FormName = string.Empty;
            SelectedPrintStationId = 0;
            _originalPrintStationId = null;
        }

        private async Task LoadLinkedPrintStationAsync(int categoryId)
        {
            SelectedPrintStationId = 0;
            _originalPrintStationId = null;
            try
            {
                var result = await _printStationApiService.GetStationsByCategoryIdAsync(categoryId);
                if (result.IsSuccess && result.Data != null && result.Data.Any())
                {
                    var linkedStation = result.Data.First();
                    SelectedPrintStationId = linkedStation.Id;
                    _originalPrintStationId = linkedStation.Id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading linked station: {ex.Message}");
            }
        }

        private async Task SyncPrintStationLinkAsync(int categoryId)
        {
            int? newStationId = (SelectedPrintStationId == 0) ? null : SelectedPrintStationId;
            if (newStationId != _originalPrintStationId)
            {
                if (_originalPrintStationId.HasValue && _originalPrintStationId.Value > 0)
                {
                    await _printStationApiService.UnlinkCategoryFromStationAsync(categoryId, _originalPrintStationId.Value);
                }
                if (newStationId.HasValue && newStationId.Value > 0)
                {
                    await _printStationApiService.LinkCategoryToStationAsync(categoryId, newStationId.Value);
                }
            }
        }

        private async Task SaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(FormName)) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                if (IsEditMode && SelectedCategory != null)
                {
                    var dto = new CategoryUpdateDto { Id = SelectedCategory.Id, Name = FormName };
                    var result = await _categoryApiService.UpdateAsync(SelectedCategory.Id, dto);
                    if (result.IsSuccess)
                    {
                        var cat = Categories.FirstOrDefault(c => c.Id == SelectedCategory.Id);
                        if (cat != null)
                        {
                            cat.Name = FormName;
                        }

                        await SyncPrintStationLinkAsync(SelectedCategory.Id);

                        CancelForm();
                        ApplySearch();
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل تعديل التصنيف.";
                    }
                }
                else
                {
                    var dto = new CategoryCreateDto { Name = FormName };
                    var result = await _categoryApiService.CreateAsync(dto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        Categories.Add(result.Data);

                        if (SelectedPrintStationId.HasValue && SelectedPrintStationId.Value > 0)
                        {
                            await _printStationApiService.LinkCategoryToStationAsync(result.Data.Id, SelectedPrintStationId.Value);
                        }

                        CancelForm();
                        ApplySearch();
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل إنشاء التصنيف.";
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

        private async Task DeleteCategoryAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف هذا التصنيف؟ قد يؤدي هذا لحذف المنتجات التابعة له.",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _categoryApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var cat = Categories.FirstOrDefault(c => c.Id == id);
                    if (cat != null)
                    {
                        Categories.Remove(cat);
                    }
                    CancelForm();
                    ApplySearch();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل حذف التصنيف.";
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
