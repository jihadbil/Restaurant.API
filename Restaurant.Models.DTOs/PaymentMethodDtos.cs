using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات طريقة الدفع (القراءة)
/// </summary>
public class PaymentMethodDto
{
    /// <summary>
    /// معرف طريقة الدفع
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// اسم طريقة الدفع
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// هل هناك ضريبة على طريقة الدفع
    /// </summary>
    public bool IsTaxFree { get; set; }

    /// <summary>
    /// رابط شعار طريقة الدفع
    /// </summary>
    public string? LogoUrl { get; set; }
}

/// <summary>
/// ناقل بيانات إنشاء طريقة دفع جديدة
/// </summary>
public class PaymentMethodCreateDto
{
    /// <summary>
    /// اسم طريقة الدفع
    /// </summary>
    [Required(ErrorMessage = "اسم طريقة الدفع مطلوب")]
    [MaxLength(50, ErrorMessage = "اسم طريقة الدفع لا يمكن أن يتجاوز 50 حرف")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// هل هناك ضريبة على طريقة الدفع
    /// </summary>
    public bool IsTaxFree { get; set; } = false;

    /// <summary>
    /// رابط شعار طريقة الدفع
    /// </summary>
    [MaxLength(500, ErrorMessage = "رابط الشعار لا يمكن أن يتجاوز 500 حرف")]
    public string? LogoUrl { get; set; }
}

/// <summary>
/// ناقل بيانات تعديل طريقة دفع موجودة
/// </summary>
public class PaymentMethodUpdateDto
{
    /// <summary>
    /// معرف طريقة الدفع
    /// </summary>
    [Required(ErrorMessage = "معرف طريقة الدفع مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// اسم طريقة الدفع الجديد
    /// </summary>
    [Required(ErrorMessage = "اسم طريقة الدفع مطلوب")]
    [MaxLength(50, ErrorMessage = "اسم طريقة الدفع لا يمكن أن يتجاوز 50 حرف")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// هل هناك ضريبة على طريقة الدفع
    /// </summary>
    public bool IsTaxFree { get; set; }

    /// <summary>
    /// رابط شعار طريقة الدفع الجديد
    /// </summary>
    [MaxLength(500, ErrorMessage = "رابط الشعار لا يمكن أن يتجاوز 500 حرف")]
    public string? LogoUrl { get; set; }
}
