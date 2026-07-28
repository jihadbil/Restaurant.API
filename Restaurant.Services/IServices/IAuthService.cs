using Restaurant.Models.DTOs;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

/// <summary>
/// واجهة خدمة التحقق والمصادقة وإدارة المستخدمين
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// تسجيل مستخدم جديد
    /// </summary>
    /// <param name="registerDto">بيانات التسجيل</param>
    /// <returns>نتيجة عملية التحقق وتفاصيل الحساب المولد</returns>
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerDto);

    /// <summary>
    /// تسجيل الدخول وإنتاج رمز JWT
    /// </summary>
    /// <param name="loginDto">بيانات تسجيل الدخول</param>
    /// <returns>رمز الـ JWT وبيانات المستخدم إذا تمت العملية بنجاح</returns>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto loginDto);
}
