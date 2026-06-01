using System;
using System.Collections.Generic;
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
    public int Id { get; set; }

    /// <summary>
    /// الكمية من المنتج في عنصر الطلب
    /// </summary>
    public int Quentity { get; set; }
    /// <summary>
    /// سعر بيع الوحدة من المنتج في عنصر الطلب
    /// </summary>
    public decimal UnitSalePrice { get; set; }
    /// <summary>
    /// سعر تكلفة الوحدة من المنتج في عنصر الطلب
    /// </summary>
    public decimal UnitCostPrice { get; set; }
    /// <summary>
    /// سعر التخفيض للوحدة من المنتج في عنصر الطلب
    /// </summary>
    public decimal UnitDiscount { get; set; }
    /// <summary>
    /// الاجمالي سعر بيع عنصر الطلب (الكمية * سعر بيع الوحدة - التخفيض)
    /// </summary>
    public decimal Total { get; set; }
    /// <summary>
    /// ملاحظات اضافية عن عنصر الطلب، مثل طلبات خاصة أو تعديلات على المنتج (مثل "بدون بصل" أو "اضافة جبنة")
    /// </summary>
    public string Notes { get; set; }



    /////////////////////////////////
    ///

    /// <summary>
    /// معرف الطلب الذي ينتمي له عنصر الطلب
    /// </summary>
    public int OrderId { get; set; }
    public Order Order { get; set; }
    /// <summary>
    /// معرف المنتج الذي ينتمي له عنصر الطلب
    /// </summary>
    public int ProductId { get; set; }
    public Product Product { get; set; }



}
