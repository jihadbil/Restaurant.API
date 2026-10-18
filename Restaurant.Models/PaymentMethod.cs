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
    public bool IsActive { get; set; } = true;


    /// <summary>
    /// معرف طريقة الدفع
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// اسم طريقة الدفع
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    /// <summary>
    /// هل هناك ضريبة على طريقة الدفع
    /// </summary>
    public bool IsTaxFree { get; set; }=false;

    public bool IsCash { get; set; } = true;

    /// <summary>
    /// رابط شعار طريقة الدفع
    /// </summary>
    [MaxLength(500)]
    public string? LogoUrl { get; set; }


    //////////////////////////////////////////////////////


    /// <summary>
    /// العلاقة مع جدول حركات الخزينة
    /// </summary>
    public ICollection<CashDrawerEntry> CashDrawerEntries { get; set; } = new List<CashDrawerEntry>();
}
