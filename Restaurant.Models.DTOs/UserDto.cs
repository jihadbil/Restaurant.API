using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ناقل بيانات المستخدم (القراءة)
/// </summary>
public class UserDto
{
    /// <summary>
    /// معرف المستخدم
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// اسم المستخدم
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// البريد الإلكتروني للمستخدم
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// رقم الهاتف للمستخدم
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// أدوار وصلاحيات المستخدم في النظام
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = new List<string>();

    /// <summary>
    /// أدوار وصلاحيات المستخدم في النظام معروضة كنص مفصول بفاصلة
    /// </summary>
    public string RolesDisplay => Roles != null ? string.Join(", ", Roles) : string.Empty;

    /// <summary>
    /// صلاحيات الوصول التفصيلية للمستخدم (أذونات الصفحات)
    /// </summary>
    public IEnumerable<string> Permissions { get; set; } = new List<string>();

    /// <summary>
    /// معرف المطعم التابع له المستخدم
    /// </summary>
    public int? RestaurantId { get; set; }
}

/// <summary>
/// ناقل بيانات تعديل بيانات مستخدم موجود
/// </summary>
public class UserUpdateDto
{
    /// <summary>
    /// اسم المستخدم الجديد
    /// </summary>
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم المستخدم لا يمكن أن يتجاوز 100 حرف")]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// البريد الإلكتروني الجديد
    /// </summary>
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// رقم الهاتف الجديد
    /// </summary>
    [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// قائمة الصلاحيات وأذونات الصفحات الجديدة للمستخدم
    /// </summary>
    public IEnumerable<string>? Permissions { get; set; }
}

/// <summary>
/// ناقل بيانات تغيير كلمة مرور مستخدم
/// </summary>
public class UserChangePasswordDto
{
    /// <summary>
    /// كلمة المرور الحالية
    /// </summary>
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
    public string CurrentPassword { get; set; } = null!;

    /// <summary>
    /// كلمة المرور الجديدة
    /// </summary>
    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(8, ErrorMessage = "يجب أن تكون كلمة المرور 8 أرقام/حروف على الأقل")]
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// ناقل بيانات تحديث أدوار وصلاحيات المستخدم
/// </summary>
public class UserRoleUpdateDto
{
    /// <summary>
    /// قائمة بالأدوار والصلاحيات المراد إسنادها للمستخدم
    /// </summary>
    [Required(ErrorMessage = "قائمة الأدوار مطلوبة")]
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// ناقل بيانات إعادة تعيين كلمة مرور مستخدم من قبل المسؤول
/// </summary>
public class UserResetPasswordDto
{
    /// <summary>
    /// كلمة المرور الجديدة
    /// </summary>
    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(8, ErrorMessage = "يجب أن تكون كلمة المرور 8 أرقام/حروف على الأقل")]
    public string NewPassword { get; set; } = null!;
}
