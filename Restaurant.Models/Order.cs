using Restaurant.Models.Enums;
using System;
using System.Collections.Generic;
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

    public DateTime Date { get; set; }
    /// <summary>
    /// حالة الطلب
    /// </summary>
    public OrderStatus OrderStatus { get; set; }

    /// <summary>
    /// ملاحظات اضافية عن الطلب
    /// </summary>
    public string Notes { get; set; }


    ///////////////////////المفاتيح الخاريجية////////////////////////

    /// <summary>
    /// معرف وسيلة الدفع المستخدمة في الطلب
    /// </summary>
    public int PaymentMethodId { get; set; }
    public PaymentMethod paymentMethod { get; set; }
    /// <summary>
    /// معرف المستخدم الدي انشا الطلب
    /// </summary>
    public int UserId { get; set; } 
    public ApplicationUser User { get; set; }

    ////////////////////////العلاقات/////////////////////////////////
    /// <summary>
    ///   العلاقة مع جدول المنتجات في الطلب
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); 

}
