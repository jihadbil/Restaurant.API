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
    public class TreasuryViewModel : BaseViewModel
    {
        private readonly ICashboxApiService _cashboxApiService;
        private readonly ICashDrawerEntryApiService _entryApiService;

        // Collections
        public ObservableCollection<CashDrawerEntryDto> Entries { get; } = new();
        public ObservableCollection<CashboxDto> Cashboxes { get; } = new();

        // Filters
        private int? _filterCashboxId;
        public int? FilterCashboxId
        {
            get => _filterCashboxId;
            set
            {
                if (SetProperty(ref _filterCashboxId, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private DateTime _filterFrom = DateTime.Today;
        public DateTime FilterFrom
        {
            get => _filterFrom;
            set
            {
                if (SetProperty(ref _filterFrom, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private DateTime _filterTo = DateTime.Today.AddDays(1).AddSeconds(-1);
        public DateTime FilterTo
        {
            get => _filterTo;
            set
            {
                if (SetProperty(ref _filterTo, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        // Manual Entry Form
        private bool _isManualEntryFormVisible;
        public bool IsManualEntryFormVisible
        {
            get => _isManualEntryFormVisible;
            set => SetProperty(ref _isManualEntryFormVisible, value);
        }

        private CashDrawerEntryType _manualEntryType = CashDrawerEntryType.Inflow;
        public CashDrawerEntryType ManualEntryType
        {
            get => _manualEntryType;
            set
            {
                if (SetProperty(ref _manualEntryType, value))
                {
                    OnPropertyChanged(nameof(ManualEntryTitle));
                }
            }
        }

        public string ManualEntryTitle => ManualEntryType == CashDrawerEntryType.Inflow ? "إيداع يدوي نقدي" : "سحب يدوي نقدي";

        private decimal _manualEntryAmount;
        public decimal ManualEntryAmount
        {
            get => _manualEntryAmount;
            set
            {
                if (SetProperty(ref _manualEntryAmount, value))
                {
                    SaveManualEntryCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string? _manualEntryNotes;
        public string? ManualEntryNotes
        {
            get => _manualEntryNotes;
            set => SetProperty(ref _manualEntryNotes, value);
        }

        // Summaries
        private decimal _totalInflow;
        public decimal TotalInflow
        {
            get => _totalInflow;
            private set => SetProperty(ref _totalInflow, value);
        }

        private decimal _totalOutflow;
        public decimal TotalOutflow
        {
            get => _totalOutflow;
            private set => SetProperty(ref _totalOutflow, value);
        }

        private decimal _netBalance;
        public decimal NetBalance
        {
            get => _netBalance;
            private set => SetProperty(ref _netBalance, value);
        }

        // Commands
        public AsyncRelayCommand LoadDataCommand { get; }
        public AsyncRelayCommand ApplyFiltersCommand { get; }
        public RelayCommand ShowManualInflowFormCommand { get; }
        public RelayCommand ShowManualOutflowFormCommand { get; }
        public RelayCommand CancelManualEntryCommand { get; }
        public AsyncRelayCommand SaveManualEntryCommand { get; }
        public AsyncRelayCommand<int> DeleteEntryCommand { get; }

        public TreasuryViewModel(ICashboxApiService cashboxApiService, ICashDrawerEntryApiService entryApiService)
        {
            _cashboxApiService = cashboxApiService;
            _entryApiService = entryApiService;

            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync);
            ShowManualInflowFormCommand = new RelayCommand(ShowManualInflowForm);
            ShowManualOutflowFormCommand = new RelayCommand(ShowManualOutflowForm);
            CancelManualEntryCommand = new RelayCommand(CancelManualEntry);
            SaveManualEntryCommand = new AsyncRelayCommand(SaveManualEntryAsync, CanSaveManualEntry);
            DeleteEntryCommand = new AsyncRelayCommand<int>(DeleteEntryAsync);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var cashboxesResult = await _cashboxApiService.GetAllAsync();
                if (cashboxesResult.IsSuccess && cashboxesResult.Data != null)
                {
                    Cashboxes.Clear();
                    foreach (var c in cashboxesResult.Data)
                    {
                        Cashboxes.Add(c);
                    }

                    // Set default cashbox from settings if set
                    if (AppSettings.Instance.DefaultCashboxId.HasValue)
                    {
                        _filterCashboxId = AppSettings.Instance.DefaultCashboxId.Value;
                        OnPropertyChanged(nameof(FilterCashboxId));
                    }
                    else if (Cashboxes.Any())
                    {
                        _filterCashboxId = Cashboxes.First().Id;
                        OnPropertyChanged(nameof(FilterCashboxId));
                    }
                }
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ في جلب بيانات الخزينة: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyFiltersAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var entriesResult = await _entryApiService.GetAllAsync(FilterCashboxId, FilterFrom, FilterTo);
                if (entriesResult.IsSuccess && entriesResult.Data != null)
                {
                    Entries.Clear();
                    foreach (var entry in entriesResult.Data)
                    {
                        Entries.Add(entry);
                    }
                    RecalculateSummary();
                }
                else
                {
                    ErrorMessage = entriesResult.ErrorMessage ?? "فشل جلب سجل الحركات.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ في الفلترة: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RecalculateSummary()
        {
            decimal inflow = 0;
            decimal outflow = 0;

            foreach (var entry in Entries)
            {
                if (entry.EntryType == CashDrawerEntryType.Outflow)
                {
                    outflow += entry.Amount;
                }
                else
                {
                    inflow += entry.Amount;
                }
            }

            TotalInflow = inflow;
            TotalOutflow = outflow;
            NetBalance = inflow - outflow;
        }

        private void ShowManualInflowForm()
        {
            ManualEntryType = CashDrawerEntryType.Inflow;
            ManualEntryAmount = 0;
            ManualEntryNotes = string.Empty;
            IsManualEntryFormVisible = true;
        }

        private void ShowManualOutflowForm()
        {
            ManualEntryType = CashDrawerEntryType.Outflow;
            ManualEntryAmount = 0;
            ManualEntryNotes = string.Empty;
            IsManualEntryFormVisible = true;
        }

        private void CancelManualEntry()
        {
            IsManualEntryFormVisible = false;
            ManualEntryAmount = 0;
            ManualEntryNotes = string.Empty;
        }

        private bool CanSaveManualEntry() => !IsBusy && ManualEntryAmount > 0 && FilterCashboxId.HasValue;

        private async Task SaveManualEntryAsync()
        {
            if (!CanSaveManualEntry()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var currentUserId = SessionManager.Instance.CurrentUser?.Id;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    ErrorMessage = "المستخدم الحالي غير معروف. يرجى إعادة تسجيل الدخول.";
                    return;
                }

                var dto = new CashDrawerEntryCreateDto
                {
                    CashboxId = FilterCashboxId!.Value,
                    Amount = ManualEntryAmount,
                    EntryType = ManualEntryType,
                    Notes = ManualEntryNotes,
                    UserId = currentUserId
                };

                var result = await _entryApiService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    Entries.Add(result.Data);
                    RecalculateSummary();
                    CancelManualEntry();
                    await ApplyFiltersAsync(); // Refresh list to get fully loaded relation names
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تسجيل الحركة اليدوية.";
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

        private async Task DeleteEntryAsync(int id)
        {
            var msgResult = System.Windows.MessageBox.Show(
                "هل أنت متأكد من رغبتك في حذف حركة درج النقود هذه؟ قد يؤدي هذا لتأثير على المطابقة المالية للمبيعات.",
                "تأكيد حذف حركة",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (msgResult != System.Windows.MessageBoxResult.Yes) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _entryApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var entry = Entries.FirstOrDefault(e => e.Id == id);
                    if (entry != null)
                    {
                        Entries.Remove(entry);
                        RecalculateSummary();
                    }
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل حذف الحركة.";
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
}
