using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models;

/// <summary>
/// جدول الخزائن
/// </summary>
public class Cashbox
{
    /// <summary>
    /// معرف الخزينة الفريد
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// اسم الخزينة
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// وصف الخزينة
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// الرصيد الافتتاحي
    /// </summary>
    [Precision(18, 2)]
    public decimal InitialBalance { get; set; } = 0;

    /// <summary>
    /// هل الخزينة نشطة
    /// </summary>
    public bool IsActive { get; set; } = true;

    ////////////////////////العلاقات/////////////////////////////////
    
    /// <summary>
    /// الحركات المرتبطة بالخزينة
    /// </summary>
    public ICollection<CashDrawerEntry> CashDrawerEntries { get; set; } = new List<CashDrawerEntry>();
}
