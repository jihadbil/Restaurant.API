using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models;

public class CashShift
{
    public int Id { get; set; }
    public int SourceCashboxId { get; set; }
    public Cashbox SourceCashbox { get; set; } = null!;
    public int TargetCashboxId { get; set; }
    public Cashbox TargetCashbox { get; set; } = null!;
    public int PreviousShiftId { get; set; }
    public int LastEntryId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ClosedAt { get; set; }
    [Precision(18, 2)] public decimal ExpectedCash { get; set; }
    [Precision(18, 2)] public decimal ActualCash { get; set; }
    [Precision(18, 2)] public decimal Difference { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [Required, MaxLength(100)] public string ClientRequestId { get; set; } = null!;
    [Required] public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<CashDrawerEntry> Entries { get; set; } = new List<CashDrawerEntry>();
}
