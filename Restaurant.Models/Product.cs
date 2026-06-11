using Microsoft.EntityFrameworkCore;
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
    [MaxLength(50)]
    public string? BarCode { get; set; }
    /// <summary>
    /// اسم المنتج
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; set; }= null!;
    /// <summary>
    /// وصف المنتج
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }



    /// <summary>
    /// سعر التكلفة للمنتج
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal CostPrice { get; set; }
    /// <summary>
    /// سعر البيع للمنتج
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal SalePrice { get; set; }
    /// <summary>
    /// رابط صورة النتج
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }



    //////////////////////////////المفاتيح الخارجية////////////////////////////////////////



    /// <summary>
    ///     معرف التصنيف الذي ينتمي له المنتج
    /// </summary>
    [Required]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;



    /////////////////////////////العلاقات//////////////////////////////////////
    /// <summary>
    /// العلاقة مع جدول عناصر الطلبات
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); 
}
