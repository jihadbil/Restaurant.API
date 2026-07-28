using Microsoft.EntityFrameworkCore;
using Restaurant.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models;

/// <summary>
/// جدول حركات درج النقود (الخزينة)
/// </summary>
public class CashDrawerEntry
{
    /// <summary>
    /// معرف الحركة الفريد
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// تاريخ ووقت الحركة
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// مبلغ الحركة
    /// </summary>
    [Required]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    /// <summary>
    /// نوع الحركة
    /// </summary>
    [Required]
    public CashDrawerEntryType EntryType { get; set; }

    /// <summary>
    /// ملاحظات عن الحركة
    /// </summary>
    public string? Notes { get; set; }

    ///////////////////////المفاتيح الخارجية والعلاقات////////////////////////

    /// <summary>
    /// معرف الخزينة المرتبطة
    /// </summary>
    [Required]
    public int CashboxId { get; set; }
    public Cashbox Cashbox { get; set; } = null!;

    /// <summary>
    /// معرف طريقة الدفع (اختياري، في حالة المبيعات مثلاً)
    /// </summary>
    public int? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// معرف الطلب المرتبط (اختياري، في حالة حركات المبيعات)
    /// </summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>
    /// معرف المستخدم الذي قام بالحركة
    /// </summary>
    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
