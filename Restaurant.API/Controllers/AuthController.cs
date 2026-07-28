using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

/// <summary>
/// وحدة تحكم عمليات التحقق وإدارة الهوية والمستخدمين
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// تسجيل حساب مستخدم جديد في النظام
    /// </summary>
    /// <param name="registerDto">نموذج بيانات التسجيل</param>
    /// <returns>بيانات المستخدم الجديد مع رمز الـ JWT لولوج النظام تلقائياً</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(registerDto);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// تسجيل الدخول إلى النظام باستخدام اسم المستخدم/البريد وكلمة المرور
    /// </summary>
    /// <param name="loginDto">نموذج تسجيل الدخول</param>
    /// <returns>بيانات المستخدم ورمز الـ JWT الخاص بالمصادقة</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(loginDto);
        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }
}
