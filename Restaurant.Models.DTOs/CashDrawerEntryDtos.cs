using Restaurant.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات حركة الصندوق (القراءة)
/// </summary>
public class CashDrawerEntryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public CashDrawerEntryType EntryType { get; set; }
    public string? Notes { get; set; }

    public int CashboxId { get; set; }
    public string CashboxName { get; set; } = string.Empty;

    public int? PaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }

    public int? OrderId { get; set; }
    public int? OrderNumber { get; set; }

    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// ملخص حركة الصندوق (لربطها مع تفاصيل الطلب)
/// </summary>
public class CashDrawerEntrySummaryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public CashDrawerEntryType EntryType { get; set; }
    public string? Notes { get; set; }
    public string CashboxName { get; set; } = string.Empty;
    public string? PaymentMethodName { get; set; }
}

/// <summary>
/// ناقل بيانات إنشاء حركة صندوق جديدة
/// </summary>
public class CashDrawerEntryCreateDto
{
    [Required(ErrorMessage = "معرف الخزينة مطلوب")]
    public int CashboxId { get; set; }

    [Required(ErrorMessage = "قيمة الحركة مطلوبة")]
    [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن تكون قيمة الحركة أكبر من صفر")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "نوع الحركة مطلوب")]
    public CashDrawerEntryType EntryType { get; set; }

    [MaxLength(500, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 500 حرف")]
    public string? Notes { get; set; }

    public int? PaymentMethodId { get; set; }
    public int? OrderId { get; set; }

    [Required(ErrorMessage = "معرف المستخدم مطلوب")]
    public string UserId { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات تعديل حركة صندوق موجودة
/// </summary>
public class CashDrawerEntryUpdateDto
{
    [Required(ErrorMessage = "معرف الحركة مطلوب")]
    public int Id { get; set; }

    [MaxLength(500, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 500 حرف")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "قيمة الحركة مطلوبة")]
    [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن تكون قيمة الحركة أكبر من صفر")]
    public decimal Amount { get; set; }
}
