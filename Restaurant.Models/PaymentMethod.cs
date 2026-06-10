using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// اسم طريقة الدفع
    /// </summary>
    [Required]
    public string Name { get; set; }=null!;
    /// <summary>
    /// هل هناك ضريبة على طريقة الدفع
    /// </summary>
    public bool IsTaxFree { get; set; }=false;


    //////////////////////////////////////////////////////


    /// <summary>
    /// العلاقة مع جدول الطلبات
    /// </summary>
    public ICollection<Order> Orders { get; set; }=new List<Order>();



}
