using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات المطعم (القراءة)
/// </summary>
public class RestaurantDto
{
    /// <summary>
    /// المعرف الفريد للمطعم
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// اسم المطعم
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// رابط شعار المطعم
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// عنوان المطعم
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// رقم الهاتف للمطعم
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// الرقم الضريبي للمطعم
    /// </summary>
    public string? TaxNumber { get; set; }
}

/// <summary>
/// ناقل بيانات إنشاء مطعم جديد
/// </summary>
public class RestaurantCreateDto
{
    /// <summary>
    /// اسم المطعم
    /// </summary>
    [Required(ErrorMessage = "اسم المطعم مطلوب")]
    [MaxLength(150, ErrorMessage = "اسم المطعم لا يمكن أن يتجاوز 150 حرف")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// رابط شعار المطعم
    /// </summary>
    [MaxLength(500, ErrorMessage = "رابط الشعار لا يمكن أن يتجاوز 500 حرف")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// عنوان المطعم
    /// </summary>
    [MaxLength(250, ErrorMessage = "العنوان لا يمكن أن يتجاوز 250 حرف")]
    public string? Address { get; set; }

    /// <summary>
    /// رقم الهاتف للمطعم
    /// </summary>
    [MaxLength(50, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 50 حرف")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// الرقم الضريبي للمطعم
    /// </summary>
    [MaxLength(50, ErrorMessage = "الرقم الضريبي لا يمكن أن يتجاوز 50 حرف")]
    public string? TaxNumber { get; set; }
}

/// <summary>
/// ناقل بيانات تعديل مطعم موجود
/// </summary>
public class RestaurantUpdateDto
{
    /// <summary>
    /// المعرف الفريد للمطعم
    /// </summary>
    [Required(ErrorMessage = "معرف المطعم مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// اسم المطعم
    /// </summary>
    [Required(ErrorMessage = "اسم المطعم مطلوب")]
    [MaxLength(150, ErrorMessage = "اسم المطعم لا يمكن أن يتجاوز 150 حرف")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// رابط شعار المطعم
    /// </summary>
    [MaxLength(500, ErrorMessage = "رابط الشعار لا يمكن أن يتجاوز 500 حرف")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// عنوان المطعم
    /// </summary>
    [MaxLength(250, ErrorMessage = "العنوان لا يمكن أن يتجاوز 250 حرف")]
    public string? Address { get; set; }

    /// <summary>
    /// رقم الهاتف للمطعم
    /// </summary>
    [MaxLength(50, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 50 حرف")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// الرقم الضريبي للمطعم
    /// </summary>
    [MaxLength(50, ErrorMessage = "الرقم الضريبي لا يمكن أن يتجاوز 50 حرف")]
    public string? TaxNumber { get; set; }
}
