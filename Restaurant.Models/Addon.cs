using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models;

/// <summary>
/// جدول الإضافات في المطعم، يحتوي على معلومات عن كل إضافة مثل اسمها ومعرفها.
/// يتيح هذا الجدول إمكانية تحديد الإضافات والطلبات الخاصة بالزبون على الأصناف.
/// </summary>
public class Addon
{
    /// <summary>
    /// معرف الإضافة
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// اسم الإضافة
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
}
