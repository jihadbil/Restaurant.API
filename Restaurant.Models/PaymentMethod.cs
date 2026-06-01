using System;
using System.Collections.Generic;
using System.Text;

namespace Restaurant.Models;
/// <summary>
/// جدول طرق الدفع
/// </summary>
public class PaymentMethod
{


    /// <summary>
    /// معرف طريقة الدفع
    /// </summary>
    public int Id { get; set; } 
    /// <summary>
    /// اسم طريقة الدفع
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// هل هناك ضريبة على طريقة الدفع
    /// </summary>
    public bool IsTaxFree { get; set; }


    //////////////////////////////////////////////////////


    /// <summary>
    /// العلاقة مع جدول الطلبات
    /// </summary>
    public ICollection<Order> Orders { get; set; }=new List<Order>();



}
