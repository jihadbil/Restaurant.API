using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models.DTOs;

/// <summary>
/// نموذج طلب تسجيل مستخدم جديد
/// </summary>
public class RegisterRequestDto
{
    /// <summary>
    /// اسم المستخدم
    /// </summary>
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [MaxLength(100, ErrorMessage = "اسم المستخدم لا يمكن أن يتجاوز 100 حرفًا")]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// البريد الإلكتروني للمستخدم
    /// </summary>
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// كلمة المرور
    /// </summary>
    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [MinLength(8, ErrorMessage = "يجب أن لا تقل كلمة المرور عن 8 أحرف")]
    public string Password { get; set; } = null!;

    /// <summary>
    /// رقم الهاتف (اختياري)
    /// </summary>
    [Phone(ErrorMessage = "صيغة رقم الهاتف غير صحيحة")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// قائمة الصلاحيات وأذونات الوصول الأولية للمستخدم
    /// </summary>
    public IEnumerable<string>? Permissions { get; set; }
}

/// <summary>
/// نموذج طلب تسجيل الدخول
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// اسم المستخدم أو البريد الإلكتروني
    /// </summary>
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// كلمة المرور
    /// </summary>
    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    public string Password { get; set; } = null!;
}

/// <summary>
/// نتيجة عملية التحقق والمصادقة
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// حالة نجاح العملية
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// رسائل توضيحية أو أخطاء
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// رمز الـ JWT المولد
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// تاريخ انتهاء صلاحية الرمز
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// بيانات المستخدم
    /// </summary>
    public UserDto? User { get; set; }
}
