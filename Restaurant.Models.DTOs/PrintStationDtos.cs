using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات محطة الطباعة (القراءة)
/// </summary>
public class PrintStationDto
{
    /// <summary>
    /// معرف محطة الطباعة
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// اسم محطة الطباعة
    /// </summary>
    public string Name { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات إنشاء محطة طباعة جديدة
/// </summary>
public class PrintStationCreateDto
{
    /// <summary>
    /// اسم محطة الطباعة
    /// </summary>
    [Required(ErrorMessage = "اسم محطة الطباعة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم محطة الطباعة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات تعديل محطة طباعة موجودة
/// </summary>
public class PrintStationUpdateDto
{
    /// <summary>
    /// معرف محطة الطباعة
    /// </summary>
    [Required(ErrorMessage = "معرف محطة الطباعة مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// اسم محطة الطباعة الجديد
    /// </summary>
    [Required(ErrorMessage = "اسم محطة الطباعة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم محطة الطباعة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;
}
