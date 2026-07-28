using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات الفئة (القراءة)
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// معرف التصنيف
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// اسم التصنيف
    /// </summary>
    public string Name { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات إنشاء فئة جديدة
/// </summary>
public class CategoryCreateDto
{
    /// <summary>
    /// اسم التصنيف
    /// </summary>
    [Required(ErrorMessage = "اسم التصنيف مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم التصنيف لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات تعديل فئة موجودة
/// </summary>
public class CategoryUpdateDto
{
    /// <summary>
    /// معرف التصنيف
    /// </summary>
    [Required(ErrorMessage = "معرف التصنيف مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// اسم التصنيف الجديد
    /// </summary>
    [Required(ErrorMessage = "اسم التصنيف مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم التصنيف لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;
}
