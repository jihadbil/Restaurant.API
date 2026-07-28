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
    public class ShiftCloseViewModel : BaseViewModel
    {
        private readonly ICashboxApiService _cashboxApiService;
        private readonly ICashDrawerEntryApiService _entryApiService;

        // Shift summary details
        private decimal _totalSales;
        public decimal TotalSales
        {
            get => _totalSales;
            private set => SetProperty(ref _totalSales, value);
        }

        private decimal _totalManualInflow;
        public decimal TotalManualInflow
        {
            get => _totalManualInflow;
            private set => SetProperty(ref _totalManualInflow, value);
        }

        private decimal _totalOutflow;
        public decimal TotalOutflow
        {
            get => _totalOutflow;
            private set => SetProperty(ref _totalOutflow, value);
        }

        private decimal _expectedCash;
        public decimal ExpectedCash
        {
            get => _expectedCash;
            private set => SetProperty(ref _expectedCash, value);
        }

        private int _totalOrdersCount;
        public int TotalOrdersCount
        {
            get => _totalOrdersCount;
            private set => SetProperty(ref _totalOrdersCount, value);
        }

        public ObservableCollection<PaymentSummaryItem> PaymentSummary { get; } = new();

        // Shift duration
        private DateTime _shiftFrom = DateTime.Today;
        public DateTime ShiftFrom
        {
            get => _shiftFrom;
            private set => SetProperty(ref _shiftFrom, value);
        }

        private DateTime _shiftTo = DateTime.Now;
        public DateTime ShiftTo
        {
            get => _shiftTo;
            private set => SetProperty(ref _shiftTo, value);
        }

        // Matching
        private decimal? _actualCash;
        public decimal? ActualCash
        {
            get => _actualCash;
            set
            {
                if (SetProperty(ref _actualCash, value))
                {
                    OnPropertyChanged(nameof(Difference));
                    OnPropertyChanged(nameof(HasDifference));
                    OnPropertyChanged(nameof(IsDifferenceNegative));
                    CloseShiftCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public decimal? Difference => ActualCash.HasValue ? ActualCash.Value - ExpectedCash : null;
        public bool HasDifference => Difference.HasValue && Difference.Value != 0;
        public bool IsDifferenceNegative => Difference.HasValue && Difference.Value < 0;

        private string _cashboxName = string.Empty;
        public string CashboxName
        {
            get => _cashboxName;
            private set => SetProperty(ref _cashboxName, value);
        }

        public bool CanViewTotals => HasPermission("Permission.Shift.ViewTotals");

        private bool HasPermission(string permissionName)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            if (user.Roles != null && user.Roles.Contains("Admin")) return true;
            return user.Permissions != null && user.Permissions.Contains(permissionName);
        }

        // Commands
        public AsyncRelayCommand LoadShiftSummaryCommand { get; }
        public AsyncRelayCommand CloseShiftCommand { get; }
        public RelayCommand CancelCommand { get; }

        public ShiftCloseViewModel(ICashboxApiService cashboxApiService, ICashDrawerEntryApiService entryApiService)
        {
            _cashboxApiService = cashboxApiService;
            _entryApiService = entryApiService;

            LoadShiftSummaryCommand = new AsyncRelayCommand(LoadShiftSummaryAsync);
            CloseShiftCommand = new AsyncRelayCommand(CloseShiftAsync, CanCloseShift);
            CancelCommand = new RelayCommand(ExecuteCancel);

            _ = LoadShiftSummaryAsync();
        }

        private async Task LoadShiftSummaryAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var defaultCashboxId = AppSettings.Instance.DefaultCashboxId;
                if (!defaultCashboxId.HasValue || defaultCashboxId.Value <= 0)
                {
                    ErrorMessage = "تنبيه: لم يتم ضبط خزينة افتراضية لهذا الجهاز في الإعدادات.";
                    return;
                }

                // Get cashbox balance details
                var balanceResult = await _cashboxApiService.GetBalanceAsync(defaultCashboxId.Value);
                if (balanceResult.IsSuccess && balanceResult.Data != null)
                {
                    CashboxName = balanceResult.Data.Name;
                    ExpectedCash = balanceResult.Data.CurrentBalance;
                }

                // Get entries of today
                ShiftFrom = DateTime.Today;
                ShiftTo = DateTime.Now;
                var entriesResult = await _entryApiService.GetAllAsync(defaultCashboxId.Value, ShiftFrom, ShiftTo);

                if (entriesResult.IsSuccess && entriesResult.Data != null)
                {
                    var entries = entriesResult.Data;
                    
                    TotalSales = entries.Where(e => e.EntryType == CashDrawerEntryType.SalePayment).Sum(e => e.Amount);
                    TotalManualInflow = entries.Where(e => e.EntryType == CashDrawerEntryType.Inflow).Sum(e => e.Amount);
                    TotalOutflow = entries.Where(e => e.EntryType == CashDrawerEntryType.Outflow).Sum(e => e.Amount);
                    TotalOrdersCount = entries.Where(e => e.EntryType == CashDrawerEntryType.SalePayment && e.OrderId.HasValue).Select(e => e.OrderId!.Value).Distinct().Count();

                    // Aggregate payment methods
                    PaymentSummary.Clear();
                    var groups = entries.Where(e => e.EntryType == CashDrawerEntryType.SalePayment)
                                        .GroupBy(e => e.PaymentMethodName ?? "نقد")
                                        .Select(g => new PaymentSummaryItem
                                        {
                                            PaymentMethodName = g.Key,
                                            Total = g.Sum(e => e.Amount)
                                        });

                    foreach (var item in groups)
                    {
                        PaymentSummary.Add(item);
                    }
                }
                else
                {
                    ErrorMessage = entriesResult.ErrorMessage ?? "فشل جلب الحركات المالية للوردية.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"حدث خطأ في تحميل ملخص الوردية: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCloseShift() => !IsBusy && ActualCash.HasValue && ActualCash.Value >= 0;

        private async Task CloseShiftAsync()
        {
            if (!CanCloseShift()) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var defaultCashboxId = AppSettings.Instance.DefaultCashboxId;
                var currentUserId = SessionManager.Instance.CurrentUser?.Id;

                if (!defaultCashboxId.HasValue || string.IsNullOrEmpty(currentUserId))
                {
                    ErrorMessage = "البيانات الأساسية لإغلاق الوردية مفقودة.";
                    return;
                }

                // If cashier registers shift closure, register an Outflow entry in database with the matched cash amount
                // representing delivering cash to management or bank.
                var notes = $"إغلاق الوردية وتسليم الصندوق. المبلغ الفعلي: {(ActualCash ?? 0):N2}. العجز/الزيادة: {Difference ?? 0:N2}";
                
                var dto = new CashDrawerEntryCreateDto
                {
                    CashboxId = defaultCashboxId.Value,
                    Amount = ActualCash ?? 0,
                    EntryType = CashDrawerEntryType.Outflow,
                    Notes = notes,
                    UserId = currentUserId
                };

                var result = await _entryApiService.CreateAsync(dto);
                if (result.IsSuccess)
                {
                    System.Windows.MessageBox.Show($"تم إغلاق الوردية وترحيل الصندوق بنجاح!\nالمبلغ المورّد: {(ActualCash ?? 0):N2} د.ل", "إغلاق الوردية", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تسجيل ترحيل الصندوق في قاعدة البيانات.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ أثناء إغلاق الوردية: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteCancel()
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            // Close window from View side helper or direct application windows fetch
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }
    }

    public class PaymentSummaryItem
    {
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
