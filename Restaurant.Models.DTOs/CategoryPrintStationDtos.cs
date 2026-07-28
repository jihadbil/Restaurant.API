using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات ربط التصنيفات بمحطة الطباعة (للقراءة)
/// </summary>
public class CategoryPrintStationDto
{
    /// <summary>
    /// معرف التصنيف
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// اسم التصنيف
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// معرف محطة الطباعة
    /// </summary>
    public int PrintStationId { get; set; }

    /// <summary>
    /// اسم محطة الطباعة
    /// </summary>
    public string PrintStationName { get; set; } = string.Empty;
}

/// <summary>
/// ناقل بيانات إنشاء ربط جديد بين تصنيف ومحطة طباعة
/// </summary>
public class CategoryPrintStationCreateDto
{
    /// <summary>
    /// معرف التصنيف المطلوب ربطه
    /// </summary>
    [Required(ErrorMessage = "معرف التصنيف مطلوب")]
    public int CategoryId { get; set; }

    /// <summary>
    /// معرف محطة الطباعة المطلوب ربطها
    /// </summary>
    [Required(ErrorMessage = "معرف محطة الطباعة مطلوب")]
    public int PrintStationId { get; set; }
}
