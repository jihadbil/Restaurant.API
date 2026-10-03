using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Security.Claims;
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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        int? restaurantId = int.TryParse(User.FindFirstValue("RestaurantId"), out var parsedRestaurantId)
            ? parsedRestaurantId
            : null;

        var result = await _authService.RegisterAsync(registerDto, restaurantId);
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
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
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
