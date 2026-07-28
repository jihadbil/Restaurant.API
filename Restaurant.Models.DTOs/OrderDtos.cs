using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Restaurant.Models.Enums;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات الطلب (القراءة)
/// </summary>
public class OrderDto
{
    /// <summary>
    /// معرف الطلب الفريد
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// رقم الطلب
    /// </summary>
    public int OrderNumber { get; set; }

    /// <summary>
    /// اجمالي الطلب
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// اجمالي تكلفة الطلب
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// اجمالي ربح الطلب
    /// </summary>
    public decimal Profit { get; set; }

    /// <summary>
    /// اجمالي التخفيض
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// تاريخ الطلب
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// حالة الطلب
    /// </summary>
    public OrderStatus? OrderStatus { get; set; }

    /// <summary>
    /// نوع الطلب
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// ملاحظات اضافية عن الطلب
    /// </summary>
    public string Notes { get; set; } = "لا يوجد ملاحظات";

    /// <summary>
    /// حركات درج النقود المرتبطة بالطلب
    /// </summary>
    public ICollection<CashDrawerEntrySummaryDto> CashDrawerEntries { get; set; } = new List<CashDrawerEntrySummaryDto>();

    /// <summary>
    /// معرف المستخدم الذي أنشأ الطلب
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// اسم المستخدم الذي أنشأ الطلب
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// عناصر الطلب
    /// </summary>
    public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
}

/// <summary>
/// ناقل بيانات إنشاء طلب جديد
/// </summary>
public class OrderCreateDto
{
    /// <summary>
    /// رقم الطلب
    /// </summary>
    [Required(ErrorMessage = "رقم الطلب مطلوب")]
    [Range(1, int.MaxValue, ErrorMessage = "رقم الطلب يجب أن يكون أكبر من صفر")]
    public int OrderNumber { get; set; }

    /// <summary>
    /// اجمالي التخفيض
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "التخفيض لا يمكن أن يكون سالبًا")]
    public decimal Discount { get; set; } = 0;

    /// <summary>
    /// حالة الطلب
    /// </summary>
    public OrderStatus? OrderStatus { get; set; }

    /// <summary>
    /// نوع الطلب
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// ملاحظات اضافية عن الطلب
    /// </summary>
    [MaxLength(500, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 500 حرف")]
    public string Notes { get; set; } = "لا يوجد ملاحظات";



    /// <summary>
    /// معرف المستخدم الذي أنشأ الطلب
    /// </summary>
    [Required(ErrorMessage = "معرف المستخدم مطلوب")]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// عناصر الطلب المراد إنشاؤها
    /// </summary>
    [Required(ErrorMessage = "يجب إضافة عنصر واحد على الأقل للطلب")]
    [MinLength(1, ErrorMessage = "يجب إضافة عنصر واحد على الأقل للطلب")]
    public ICollection<OrderItemCreateDto> OrderItems { get; set; } = new List<OrderItemCreateDto>();
}

/// <summary>
/// ناقل بيانات تعديل طلب موجود
/// </summary>
public class OrderUpdateDto
{
    /// <summary>
    /// معرف الطلب
    /// </summary>
    [Required(ErrorMessage = "معرف الطلب مطلوب")]
    public int Id { get; set; }

    /// <summary>
    /// حالة الطلب
    /// </summary>
    public OrderStatus? OrderStatus { get; set; }

    /// <summary>
    /// نوع الطلب
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// ملاحظات اضافية عن الطلب
    /// </summary>
    [MaxLength(500, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 500 حرف")]
    public string Notes { get; set; } = "لا يوجد ملاحظات";



    /// <summary>
    /// اجمالي التخفيض
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "التخفيض لا يمكن أن يكون سالبًا")]
    public decimal Discount { get; set; }
}
