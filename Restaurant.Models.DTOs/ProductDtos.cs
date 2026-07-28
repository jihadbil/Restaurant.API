using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات المنتج (القراءة)
/// </summary>
public class ProductDto
{
    /// <summary>
    /// المعرف الفريد للمنتج
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// باركود المنتج
    /// </summary>
    public string? BarCode { get; set; }

    /// <summary>
    /// اسم المنتج
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// وصف المنتج
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// سعر التكلفة للمنتج
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// سعر البيع للمنتج
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// رابط صورة المنتج
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// معرف التصنيف الذي ينتمي له المنتج
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// اسم التصنيف الذي ينتمي له المنتج
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// المربح (سعر البيع - سعر التكلفة)
    /// </summary>
    public decimal Profit => SalePrice - CostPrice;

    /// <summary>
    /// نسبة المربح (المربح / سعر التكلفة * 100)
    /// </summary>
    public decimal ProfitPercentage => CostPrice > 0 ? (Profit / CostPrice) * 100 : 0;
}

/// <summary>
/// ناقل بيانات إنشاء منتج جديد
/// </summary>
public class ProductCreateDto
{
    /// <summary>
    /// باركود المنتج
    /// </summary>
    [MaxLength(50, ErrorMessage = "الباركود لا يمكن أن يتجاوز 50 حرفًا")]
    public string? BarCode { get; set; }

    /// <summary>
    /// اسم المنتج
    /// </summary>
    [Required(ErrorMessage = "اسم المنتج مطلوب")]
    [MaxLength(150, ErrorMessage = "اسم المنتج لا يمكن أن يتجاوز 150 حرفًا")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// وصف المنتج
    /// </summary>
    [MaxLength(500, ErrorMessage = "وصف المنتج لا يمكن أن يتجاوز 500 حرفًا")]
    public string? Description { get; set; }

    /// <summary>
    /// سعر التكلفة للمنتج
    /// </summary>
    [Required(ErrorMessage = "سعر التكلفة مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "سعر التكلفة يجب أن يكون أكبر من صفر")]
    public decimal CostPrice { get; set; }

    /// <summary>
    /// سعر البيع للمنتج
    /// </summary>
    [Required(ErrorMessage = "سعر البيع مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "سعر البيع يجب أن يكون أكبر من صفر")]
    public decimal SalePrice { get; set; }

    /// <summary>
    /// رابط صورة المنتج
    /// </summary>
    [MaxLength(500, ErrorMessage = "رابط الصورة لا يمكن أن يتجاوز 500 حرفًا")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// معرف التصنيف الذي ينتمي له المنتج
    /// </summary>
    [Required(ErrorMessage = "معرف التصنيف مطلوب")]
    public int CategoryId { get; set; }
}

/// <summary>
/// ناقل بيانات تعديل منتج موجود
/// </summary>
public class ProductUpdateDto
{
    /// <summary>
    /// المعرف الفريد للمنتج
    /// </summary>
    [Required(ErrorMessage = "معرف المنتج مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// باركود المنتج
    /// </summary>
    [MaxLength(50, ErrorMessage = "الباركود لا يمكن أن يتجاوز 50 حرفًا")]
    public string? BarCode { get; set; }

    /// <summary>
    /// اسم المنتج
    /// </summary>
    [Required(ErrorMessage = "اسم المنتج مطلوب")]
    [MaxLength(150, ErrorMessage = "اسم المنتج لا يمكن أن يتجاوز 150 حرفًا")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// وصف المنتج
    /// </summary>
    [MaxLength(500, ErrorMessage = "وصف المنتج لا يمكن أن يتجاوز 500 حرفًا")]
    public string? Description { get; set; }

    /// <summary>
    /// سعر التكلفة للمنتج
    /// </summary>
    [Required(ErrorMessage = "سعر التكلفة مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "سعر التكلفة يجب أن يكون أكبر من صفر")]
    public decimal CostPrice { get; set; }

    /// <summary>
    /// سعر البيع للمنتج
    /// </summary>
    [Required(ErrorMessage = "سعر البيع مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "سعر البيع يجب أن يكون أكبر من صفر")]
    public decimal SalePrice { get; set; }

    /// <summary>
    /// رابط صورة المنتج
    /// </summary>
    [MaxLength(500, ErrorMessage = "رابط الصورة لا يمكن أن يتجاوز 500 حرفًا")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// معرف التصنيف الذي ينتمي له المنتج
    /// </summary>
    [Required(ErrorMessage = "معرف التصنيف مطلوب")]
    public int CategoryId { get; set; }
}
