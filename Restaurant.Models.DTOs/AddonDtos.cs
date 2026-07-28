using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات الإضافة (القراءة)
/// </summary>
public class AddonDto
{
    /// <summary>
    /// معرف الإضافة
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// اسم الإضافة
    /// </summary>
    public string Name { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات إنشاء إضافة جديدة
/// </summary>
public class AddonCreateDto
{
    /// <summary>
    /// اسم الإضافة
    /// </summary>
    [Required(ErrorMessage = "اسم الإضافة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم الإضافة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات تعديل إضافة موجودة
/// </summary>
public class AddonUpdateDto
{
    /// <summary>
    /// معرف الإضافة
    /// </summary>
    [Required(ErrorMessage = "معرف الإضافة مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// اسم الإضافة الجديد
    /// </summary>
    [Required(ErrorMessage = "اسم الإضافة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم الإضافة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;
}
