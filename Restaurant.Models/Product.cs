using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Restaurant.Models;
/// <summary>
/// جدول المنتجات
/// </summary>
public class Product
{
    /// <summary>
    /// المعرف الفريد للمنتج
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// باركود المنتج
    /// </summary>
    public string? BarCode { get; set; }
    /// <summary>
    /// اسم المنتج
    /// </summary>
    [Required]
    public string Name { get; set; }= null!;
    /// <summary>
    /// وصف المنتج
    /// </summary>
    public string? Description { get; set; }



    /// <summary>
    /// سعر التكلفة للمنتج
    /// </summary>
    [Required]
    public decimal CostPrice { get; set; }
    /// <summary>
    /// سعر البيع للمنتج
    /// </summary>
    [Required]
    public decimal SalePrice { get; set; }
    /// <summary>
    /// رابط صورة النتج
    /// </summary>
    public string? ImageUrl { get; set; }



    //////////////////////////////المفاتيح الخارجية////////////////////////////////////////



    /// <summary>
    ///     معرف التصنيف الذي ينتمي له المنتج
    /// </summary>
    [Required]
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;



    /////////////////////////////العلاقات//////////////////////////////////////
    /// <summary>
    /// العلاقة مع جدول عناصر الطلبات
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); 
}
