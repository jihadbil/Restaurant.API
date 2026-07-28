using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Services;

/// <summary>
/// تطبيق خدمة التحقق والمصادقة وتوليد الرموز المميزة
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, IMapper mapper)
    {
        _userManager = userManager;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerDto)
    {
        // التحقق من تكرار اسم المستخدم
        var userByUsername = await _userManager.FindByNameAsync(registerDto.UserName);
        if (userByUsername != null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "اسم المستخدم مسجل بالفعل."
            };
        }

        // التحقق من تكرار البريد الإلكتروني
        var userByEmail = await _userManager.FindByEmailAsync(registerDto.Email);
        if (userByEmail != null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "البريد الإلكتروني مسجل بالفعل."
            };
        }

        // إنشاء كائن المستخدم الجديد
        var user = new ApplicationUser
        {
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            PhoneNumber = registerDto.PhoneNumber
        };

        // إنشاء المستخدم وحفظه في قاعدة البيانات مع تشفير كلمة المرور
        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = new List<string>();
            foreach (var error in result.Errors)
            {
                errors.Add(error.Description);
            }
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = string.Join(" | ", errors)
            };
        }

        // إضافة صلاحيات الصفحات كـ Claims إذا تم تحديدها
        if (registerDto.Permissions != null)
        {
            foreach (var perm in registerDto.Permissions)
            {
                await _userManager.AddClaimAsync(user, new Claim("Permission", perm));
            }
        }

        // توليد رمز JWT لتسجيل الدخول التلقائي عند التسجيل بنجاح
        var token = await GenerateJwtTokenAsync(user);

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        userDto.Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "تم تسجيل المستخدم بنجاح.",
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = userDto
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginDto)
    {
        // البحث عن المستخدم بواسطة اسم المستخدم أو البريد الإلكتروني
        var user = await _userManager.FindByNameAsync(loginDto.UserName);
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(loginDto.UserName);
        }

        // التحقق من وجود المستخدم وصحة كلمة المرور
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "اسم المستخدم أو كلمة المرور غير صحيحة."
            };
        }

        // توليد رمز JWT
        var token = await GenerateJwtTokenAsync(user);

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        userDto.Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "تم تسجيل الدخول بنجاح.",
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = userDto
        };
    }

    /// <summary>
    /// توليد رمز الـ JWT وتاريخ الانتهاء للمستخدم
    /// </summary>
    private async Task<(string Token, DateTime ExpiresAt)> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "A_Very_Long_And_Super_Secret_Key_For_Restaurant_API_12345!"));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // الحصول على أدوار المستخدم (Roles) وإضافتها للـ Claims
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var durationInMinutes = Convert.ToDouble(jwtSettings["DurationInMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(durationInMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtSettings["Issuer"] ?? "RestaurantAPI",
            Audience = jwtSettings["Audience"] ?? "RestaurantClients",
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiresAt.ToLocalTime());
    }
}
