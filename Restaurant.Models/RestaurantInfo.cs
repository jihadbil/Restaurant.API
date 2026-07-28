using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models;

/// <summary>
/// جدول معلومات المطعم
/// </summary>
public class RestaurantInfo
{
    /// <summary>
    /// المعرف الفريد للمطعم
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// اسم المطعم
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// رابط شعار المطعم
    /// </summary>
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// عنوان المطعم
    /// </summary>
    [MaxLength(250)]
    public string? Address { get; set; }

    /// <summary>
    /// رقم الهاتف للمطعم
    /// </summary>
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// الرقم الضريبي للمطعم
    /// </summary>
    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    /////////////////////////////العلاقات//////////////////////////////////////
    
    /// <summary>
    /// المستخدمين التابعين للمطعم
    /// </summary>
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
