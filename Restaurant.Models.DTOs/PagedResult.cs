using Restaurant.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed class OrderQueryDto
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 200)] public int PageSize { get; set; } = 50;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    [EnumDataType(typeof(OrderStatus))] public OrderStatus? Status { get; set; }
    [MaxLength(100)] public string? Search { get; set; }
}
