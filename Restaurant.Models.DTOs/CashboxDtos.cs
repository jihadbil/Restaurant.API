using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات الخزينة (القراءة)
/// </summary>
public class CashboxDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal InitialBalance { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// ناقل بيانات إنشاء خزينة جديدة
/// </summary>
public class CashboxCreateDto
{
    [Required(ErrorMessage = "اسم الخزينة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم الخزينة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "الرصيد الافتتاحي لا يمكن أن يكون سالبًا")]
    public decimal InitialBalance { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// ناقل بيانات تعديل خزينة موجودة
/// </summary>
public class CashboxUpdateDto
{
    [Required(ErrorMessage = "معرف الخزينة مطلوب")]
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الخزينة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم الخزينة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// رصيد وحالة الخزينة الحالية
/// </summary>
public class CashboxBalanceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal InitialBalance { get; set; }
    public decimal TotalInflow { get; set; }
    public decimal TotalOutflow { get; set; }
    public decimal CurrentBalance { get; set; }
}
