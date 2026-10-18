using Restaurant.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

public sealed class CashEntryQueryDto
{
    public int? CashboxId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    [EnumDataType(typeof(CashDrawerEntryType))] public CashDrawerEntryType? EntryType { get; set; }
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 200)] public int PageSize { get; set; } = 100;
}

public sealed record CashEntryPageDto(IReadOnlyList<CashDrawerEntryDto> Items, int TotalCount, int Page, int PageSize,
    decimal TotalInflow, decimal TotalOutflow, decimal TotalSales);
