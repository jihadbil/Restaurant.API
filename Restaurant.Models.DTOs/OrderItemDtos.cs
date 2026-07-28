using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات عنصر الطلب (القراءة)
/// </summary>
public class OrderItemDto
{
    /// <summary>
    /// معرف عنصر الطلب الفريد
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// الكمية من المنتج
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// سعر بيع الوحدة من المنتج
    /// </summary>
    public decimal UnitSalePrice { get; set; }

    /// <summary>
    /// سعر تكلفة الوحدة من المنتج
    /// </summary>
    public decimal UnitCostPrice { get; set; }

    /// <summary>
    /// سعر التخفيض للوحدة من المنتج
    /// </summary>
    public decimal UnitDiscount { get; set; }

    /// <summary>
    /// الاجمالي سعر بيع عنصر الطلب (الكمية * سعر بيع الوحدة - التخفيض)
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// ملاحظات اضافية عن عنصر الطلب
    /// </summary>
    public string Notes { get; set; } = "لا يوجد ملاحظات";

    /// <summary>
    /// معرف الطلب الذي ينتمي له عنصر الطلب
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// معرف المنتج الذي ينتمي له عنصر الطلب
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// اسم المنتج
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// معرف التصنيف
    /// </summary>
    public int CategoryId { get; set; }
}

/// <summary>
/// ناقل بيانات إضافة عنصر جديد للطلب
/// </summary>
public class OrderItemCreateDto
{
    /// <summary>
    /// الكمية من المنتج
    /// </summary>
    [Required(ErrorMessage = "الكمية مطلوبة")]
    [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون 1 على الأقل")]
    public int Quantity { get; set; }

    /// <summary>
    /// سعر بيع الوحدة من المنتج
    /// </summary>
    [Required(ErrorMessage = "سعر بيع الوحدة مطلوب")]
    [Range(0, double.MaxValue, ErrorMessage = "سعر البيع لا يمكن أن يكون سالبًا")]
    public decimal UnitSalePrice { get; set; }

    /// <summary>
    /// سعر تكلفة الوحدة من المنتج
    /// </summary>
    [Required(ErrorMessage = "سعر تكلفة الوحدة مطلوب")]
    [Range(0, double.MaxValue, ErrorMessage = "سعر التكلفة لا يمكن أن يكون سالبًا")]
    public decimal UnitCostPrice { get; set; }

    /// <summary>
    /// سعر التخفيض للوحدة من المنتج
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "التخفيض لا يمكن أن يكون سالبًا")]
    public decimal UnitDiscount { get; set; } = 0;

    /// <summary>
    /// ملاحظات اضافية عن عنصر الطلب
    /// </summary>
    [MaxLength(200, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 200 حرف")]
    public string Notes { get; set; } = "لا يوجد ملاحظات";

    /// <summary>
    /// معرف المنتج الذي ينتمي له عنصر الطلب
    /// </summary>
    [Required(ErrorMessage = "معرف المنتج مطلوب")]
    public int ProductId { get; set; }
}

/// <summary>
/// ناقل بيانات تعديل عنصر طلب موجود
/// </summary>
public class OrderItemUpdateDto
{
    /// <summary>
    /// معرف عنصر الطلب
    /// </summary>
    [Required(ErrorMessage = "معرف عنصر الطلب مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// الكمية من المنتج
    /// </summary>
    [Required(ErrorMessage = "الكمية مطلوبة")]
    [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون 1 على الأقل")]
    public int Quantity { get; set; }

    /// <summary>
    /// سعر بيع الوحدة من المنتج
    /// </summary>
    [Required(ErrorMessage = "سعر بيع الوحدة مطلوب")]
    [Range(0, double.MaxValue, ErrorMessage = "سعر البيع لا يمكن أن يكون سالبًا")]
    public decimal UnitSalePrice { get; set; }

    /// <summary>
    /// سعر تكلفة الوحدة من المنتج
    /// </summary>
    [Required(ErrorMessage = "سعر تكلفة الوحدة مطلوب")]
    [Range(0, double.MaxValue, ErrorMessage = "سعر التكلفة لا يمكن أن يكون سالبًا")]
    public decimal UnitCostPrice { get; set; }

    /// <summary>
    /// سعر التخفيض للوحدة من المنتج
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "التخفيض لا يمكن أن يكون سالبًا")]
    public decimal UnitDiscount { get; set; }

    /// <summary>
    /// ملاحظات اضافية عن عنصر الطلب
    /// </summary>
    [MaxLength(200, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 200 حرف")]
    public string Notes { get; set; } = "لا يوجد ملاحظات";

    /// <summary>
    /// معرف المنتج الذي ينتمي له عنصر الطلب
    /// </summary>
    [Required(ErrorMessage = "معرف المنتج مطلوب")]
    public int ProductId { get; set; }
}
