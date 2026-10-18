using System.Collections.ObjectModel;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.ViewModels;

public class ShiftCloseViewModel : BaseViewModel
{
    private readonly ICashboxApiService _cashboxApiService;
    private CashShiftContextDto? _context;
    private CashShiftCloseDto? _pendingClose;
    public decimal TotalSales => _context?.TotalSales ?? 0;
    public decimal TotalManualInflow => _context?.TotalManualInflow ?? 0;
    public decimal TotalOutflow => _context?.TotalOutflow ?? 0;
    public decimal ExpectedCash => _context?.ExpectedCash ?? 0;
    public int TotalOrdersCount => _context?.TotalOrdersCount ?? 0;
    public DateTime ShiftFrom => _context?.StartedAt ?? DateTime.Now;
    public DateTime ShiftTo => DateTime.Now;
    public string CashboxName => _context?.SourceCashboxName ?? string.Empty;
    public bool CanViewTotals => _context?.ExpectedCash.HasValue == true;
    public ObservableCollection<PaymentSummaryItem> PaymentSummary { get; } = new();
    public ObservableCollection<CashboxDto> TargetCashboxes { get; } = new();

    private int? _targetCashboxId;
    public int? TargetCashboxId
    {
        get => _targetCashboxId;
        set { if (SetProperty(ref _targetCashboxId, value)) CloseShiftCommand.RaiseCanExecuteChanged(); }
    }

    private decimal? _actualCash;
    public decimal? ActualCash
    {
        get => _actualCash;
        set
        {
            if (!SetProperty(ref _actualCash, value)) return;
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(HasDifference));
            OnPropertyChanged(nameof(IsDifferenceNegative));
            CloseShiftCommand.RaiseCanExecuteChanged();
        }
    }
    public decimal? Difference => CanViewTotals && ActualCash.HasValue ? ActualCash.Value - ExpectedCash : null;
    public bool HasDifference => Difference.HasValue && Difference.Value != 0;
    public bool IsDifferenceNegative => Difference.HasValue && Difference.Value < 0;
    private string? _notes;
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public AsyncRelayCommand LoadShiftSummaryCommand { get; }
    public AsyncRelayCommand CloseShiftCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ShiftCloseViewModel(ICashboxApiService cashboxApiService)
    {
        _cashboxApiService = cashboxApiService;
        LoadShiftSummaryCommand = new AsyncRelayCommand(LoadShiftSummaryAsync);
        CloseShiftCommand = new AsyncRelayCommand(CloseShiftAsync, CanCloseShift);
        CancelCommand = new RelayCommand(() => { if (!IsBusy) CloseWindow(); });
        _ = LoadShiftSummaryAsync();
    }

    private async Task LoadShiftSummaryAsync()
    {
        ClearErrors();
        _context = null;
        IsBusy = true;
        CloseShiftCommand.RaiseCanExecuteChanged();
        try
        {
            var sourceId = AppSettings.Instance.DefaultCashboxId;
            var boxes = await _cashboxApiService.GetAllAsync();
            if (!boxes.IsSuccess || boxes.Data == null)
            { ErrorMessage = boxes.ErrorMessage ?? "فشل تحميل الخزائن."; return; }
            if (!boxes.Data.Any(c => c.Id == sourceId && c.IsActive && c.IsDrawer))
            { ErrorMessage = "اختر خزينة درج نشطة لهذا الجهاز من الإعدادات."; return; }
            TargetCashboxes.Clear();
            foreach (var box in boxes.Data.Where(c => c.IsActive && !c.IsDrawer && c.Id != sourceId)) TargetCashboxes.Add(box);
            TargetCashboxId = TargetCashboxes.Any(c => c.Id == TargetCashboxId) ? TargetCashboxId : TargetCashboxes.FirstOrDefault()?.Id;
            if (!TargetCashboxId.HasValue)
            { ErrorMessage = "يجب إعداد خزينة مستلمة نشطة تختلف عن الدرج."; return; }
            var context = await _cashboxApiService.GetShiftContextAsync(sourceId!.Value);
            if (!context.IsSuccess || context.Data == null)
            { ErrorMessage = context.ErrorMessage ?? "فشل تحميل بيانات الوردية."; return; }
            _context = context.Data;
            if (!_context.CanClose) ErrorMessage = "الوردية مغلقة ولا توجد حركات جديدة.";
            PaymentSummary.Clear();
            foreach (var item in _context.PaymentSummary ?? [])
                PaymentSummary.Add(new PaymentSummaryItem { PaymentMethodName = item.PaymentMethodName, Total = item.Total });
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally
        {
            IsBusy = false;
            foreach (var name in new[] { nameof(TotalSales), nameof(TotalManualInflow), nameof(TotalOutflow), nameof(ExpectedCash),
                nameof(TotalOrdersCount), nameof(ShiftFrom), nameof(ShiftTo), nameof(CashboxName), nameof(CanViewTotals), nameof(Difference), nameof(HasDifference) })
                OnPropertyChanged(name);
            CloseShiftCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanCloseShift() => !IsBusy && _context?.CanClose == true && TargetCashboxId.HasValue
        && ActualCash.HasValue && ActualCash >= 0 && decimal.Round(ActualCash.Value, 2) == ActualCash;

    private async Task CloseShiftAsync()
    {
        if (!CanCloseShift()) return;
        ClearErrors();
        IsBusy = true;
        CloseShiftCommand.RaiseCanExecuteChanged();
        try
        {
            if (_pendingClose == null || _pendingClose.ActualCash != ActualCash || _pendingClose.TargetCashboxId != TargetCashboxId
                || _pendingClose.PreviousShiftId != _context!.PreviousShiftId || _pendingClose.LastEntryId != _context.LastEntryId || _pendingClose.Notes != Notes)
                _pendingClose = new CashShiftCloseDto
                {
                    SourceCashboxId = _context!.SourceCashboxId,
                    TargetCashboxId = TargetCashboxId!.Value,
                    ActualCash = ActualCash!.Value,
                    PreviousShiftId = _context.PreviousShiftId,
                    LastEntryId = _context.LastEntryId,
                    Notes = Notes,
                    ClientRequestId = Guid.NewGuid().ToString("N")
                };
            var result = await _cashboxApiService.CloseShiftAsync(_pendingClose);
            if (!result.IsSuccess)
            {
                var recovered = await _cashboxApiService.GetClosedShiftByRequestAsync(_pendingClose.ClientRequestId);
                if (recovered.IsSuccess) result = recovered;
            }
            if (result.IsSuccess && result.Data != null)
            {
                System.Windows.MessageBox.Show($"تم حفظ إغلاق الوردية #{result.Data.Id} وتوريد {result.Data.ActualCash:N2} د.ل إلى {result.Data.TargetCashboxName}.",
                    "إغلاق الوردية", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                CloseWindow();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "فشل إغلاق الوردية.";
                if (result.StatusCode == 409)
                {
                    await LoadShiftSummaryAsync();
                    if (_context?.CanClose == true) ErrorMessage = result.ErrorMessage ?? "تغيرت حركات الدرج. أعد عدّ النقد قبل التأكيد.";
                }
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; CloseShiftCommand.RaiseCanExecuteChanged(); }
    }

    private void CloseWindow() => System.Windows.Application.Current.Dispatcher.Invoke(() =>
    {
        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            if (window.DataContext == this) { window.Close(); break; }
    });
}

public class PaymentSummaryItem
{
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
