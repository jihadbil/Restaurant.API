using Microsoft.EntityFrameworkCore;
using Restaurant.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Restaurant.Models;
/// <summary>
/// جدول الطلبات
/// </summary>
public class Order
{
    /// <summary>
    /// معرف الطلب الفريد
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// رقم الطلب
    /// </summary>
    [Required]
    public int OrderNumber { get; set; }
    /// <summary>
    /// اجمالي الطلب
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal Total { get; set; }
    /// <summary>
    /// اجمالي تكلفة الطلب
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal Cost { get; set; }
    /// <summary>
    /// اجمالي ربح الطلب 
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal Profit { get; set; }
    /// <summary>
    /// اجمالي التخفيض
    /// </summary>
    [Precision(18, 2)]
    public decimal Discount { get; set; } = 0;

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
    public string Notes { get; set; }= "لا يوجد ملاحظات";


    ///////////////////////المفاتيح الخاريجية////////////////////////

    /// <summary>
    /// معرف المستخدم الدي انشا الطلب
    /// </summary>
    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    ////////////////////////العلاقات/////////////////////////////////
    /// <summary>
    ///   العلاقة مع جدول المنتجات في الطلب
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); 

    /// <summary>
    /// حركات درج النقود المرتبطة بالطلب
    /// </summary>
    public ICollection<CashDrawerEntry> CashDrawerEntries { get; set; } = new List<CashDrawerEntry>();
}
