using System.ComponentModel.DataAnnotations;
using Restaurant.Models.Enums;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات الطابعة (القراءة)
/// </summary>
public class PrinterDto
{
    /// <summary>
    /// معرف الطابعة
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// اسم الطابعة المعروض
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// الاسم الحقيقي للطابعة في نظام التشغيل أو الشبكة
    /// </summary>
    public string PrinterName { get; set; } = null!;

    /// <summary>
    /// نوع الطابعة (طابعة فواتير، مطبخ، بار...)
    /// </summary>
    public PrinterType PrinterType { get; set; }

    /// <summary>
    /// معرف محطة الطباعة التابعة لها
    /// </summary>
    public int PrintStationId { get; set; }

    /// <summary>
    /// اسم محطة الطباعة التابعة لها
    /// </summary>
    public string PrintStationName { get; set; } = string.Empty;
}

/// <summary>
/// ناقل بيانات إنشاء طابعة جديدة
/// </summary>
public class PrinterCreateDto
{
    /// <summary>
    /// اسم الطابعة المعروض
    /// </summary>
    [Required(ErrorMessage = "اسم الطابعة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم الطابعة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// الاسم الحقيقي للطابعة في نظام التشغيل أو الشبكة
    /// </summary>
    [Required(ErrorMessage = "الاسم الحقيقي للطابعة مطلوب")]
    [MaxLength(150, ErrorMessage = "الاسم الحقيقي للطابعة لا يمكن أن يتجاوز 150 حرف")]
    public string PrinterName { get; set; } = null!;

    /// <summary>
    /// نوع الطابعة (طابعة فواتير، مطبخ، بار...)
    /// </summary>
    [Required(ErrorMessage = "نوع الطابعة مطلوب")]
    public PrinterType PrinterType { get; set; } = PrinterType.Receipt;

    /// <summary>
    /// معرف محطة الطباعة التابعة لها
    /// </summary>
    [Required(ErrorMessage = "معرف محطة الطباعة مطلوب")]
    public int PrintStationId { get; set; }
}

/// <summary>
/// ناقل بيانات تعديل طابعة موجودة
/// </summary>
public class PrinterUpdateDto
{
    /// <summary>
    /// معرف الطابعة
    /// </summary>
    [Required(ErrorMessage = "معرف الطابعة مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// اسم الطابعة المعروض
    /// </summary>
    [Required(ErrorMessage = "اسم الطابعة مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم الطابعة لا يمكن أن يتجاوز 100 حرف")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// الاسم الحقيقي للطابعة في نظام التشغيل أو الشبكة
    /// </summary>
    [Required(ErrorMessage = "الاسم الحقيقي للطابعة مطلوب")]
    [MaxLength(150, ErrorMessage = "الاسم الحقيقي للطابعة لا يمكن أن يتجاوز 150 حرف")]
    public string PrinterName { get; set; } = null!;

    /// <summary>
    /// نوع الطابعة (طابعة فواتير، مطبخ، بار...)
    /// </summary>
    [Required(ErrorMessage = "نوع الطابعة مطلوب")]
    public PrinterType PrinterType { get; set; }

    /// <summary>
    /// معرف محطة الطباعة التابعة لها
    /// </summary>
    [Required(ErrorMessage = "معرف محطة الطباعة مطلوب")]
    public int PrintStationId { get; set; }
}
