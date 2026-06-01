using System;
using System.Collections.Generic;
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
    public int Id { get; set; }
    /// <summary>
    /// باركود المنتج
    /// </summary>
    public string BarCode { get; set; }
    /// <summary>
    /// اسم المنتج
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// وصف المنتج
    /// </summary>
    public string Description { get; set; } 



    /// <summary>
    /// سعر التكلفة للمنتج
    /// </summary>
    public decimal CostPrice { get; set; }
    /// <summary>
    /// سعر البيع للمنتج
    /// </summary>
    public Decimal SalePrice { get; set; }
    /// <summary>
    /// رابط صورة النتج
    /// </summary>
    public string ImageUrl { get; set; }



    //////////////////////////////المفاتيح الخارجية////////////////////////////////////////



    /// <summary>
    ///     معرف التصنيف الذي ينتمي له المنتج
    /// </summary>
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; }



    /////////////////////////////العلاقات//////////////////////////////////////
    /// <summary>
    /// العلاقة مع جدول عناصر الطلبات
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); 
}
