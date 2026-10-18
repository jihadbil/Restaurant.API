using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

public class CashShiftContextDto
{
    public int SourceCashboxId { get; set; }
    public string SourceCashboxName { get; set; } = string.Empty;
    public int PreviousShiftId { get; set; }
    public int LastEntryId { get; set; }
    public DateTime StartedAt { get; set; }
    public bool CanClose { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? TotalSales { get; set; }
    public decimal? TotalRefunds { get; set; }
    public decimal? TotalManualInflow { get; set; }
    public decimal? TotalOutflow { get; set; }
    public int? TotalOrdersCount { get; set; }
    public List<CashShiftPaymentSummaryDto>? PaymentSummary { get; set; }
}

public class CashShiftPaymentSummaryDto
{
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class CashShiftCloseDto
{
    [Range(1, int.MaxValue)] public int SourceCashboxId { get; set; }
    [Range(1, int.MaxValue)] public int TargetCashboxId { get; set; }
    [Required(ErrorMessage = "المبلغ المعدود مطلوب"), Money] public decimal? ActualCash { get; set; }
    [Range(0, int.MaxValue)] public int PreviousShiftId { get; set; }
    [Range(0, int.MaxValue)] public int LastEntryId { get; set; }
    [Required, MaxLength(100)] public string ClientRequestId { get; set; } = null!;
    [MaxLength(500)] public string? Notes { get; set; }
}

public class CashShiftDto
{
    public int Id { get; set; }
    public int SourceCashboxId { get; set; }
    public string SourceCashboxName { get; set; } = string.Empty;
    public int TargetCashboxId { get; set; }
    public string TargetCashboxName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime ClosedAt { get; set; }
    public decimal ActualCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Difference { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
