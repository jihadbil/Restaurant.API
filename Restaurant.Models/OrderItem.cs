using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Restaurant.Models;
/// <summary>
/// جدول محتويات الطلب
/// </summary>
public class OrderItem
{
    /// <summary>
    /// معرف عنصر الطلب الفريد
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// الكمية من المنتج في عنصر الطلب
    /// </summary>
    [Required]
    public int Quantity { get; set; }
    /// <summary>
    /// سعر بيع الوحدة من المنتج في عنصر الطلب
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal UnitSalePrice { get; set; }
    /// <summary>
    /// سعر تكلفة الوحدة من المنتج في عنصر الطلب
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal UnitCostPrice { get; set; }
    /// <summary>
    /// سعر التخفيض للوحدة من المنتج في عنصر الطلب
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal UnitDiscount { get; set; }
    /// <summary>
    /// الاجمالي سعر بيع عنصر الطلب (الكمية * سعر بيع الوحدة - التخفيض)
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal Total { get; set; }
    /// <summary>
    /// ملاحظات اضافية عن عنصر الطلب، مثل طلبات خاصة أو تعديلات على المنتج (مثل "بدون بصل" أو "اضافة جبنة")
    /// </summary>
    public string Notes { get; set; }= "لا يوجد ملاحظات";



    /////////////////////////////////
    ///

    /// <summary>
    /// معرف الطلب الذي ينتمي له عنصر الطلب
    /// </summary>
    [Required]
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    /// <summary>
    /// معرف المنتج الذي ينتمي له عنصر الطلب
    /// </summary>
    [Required]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;



}
