using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} was not found." });
        }
        return Ok(user);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableRoles()
    {
        var roles = await _userService.GetAvailableRolesAsync();
        return Ok(roles);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UserUpdateDto userUpdateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _userService.UpdateUserAsync(id, userUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"User with ID {id} was not found or update failed." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"User with ID {id} was not found or deletion failed." });
        }

        return NoContent();
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] UserChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _userService.ChangePasswordAsync(id, changePasswordDto);
        if (!success)
        {
            return BadRequest(new { message = "Password change failed. Please make sure the current password is correct." });
        }

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] UserResetPasswordDto resetPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _userService.ResetPasswordAsync(id, resetPasswordDto.NewPassword);
        if (!success)
        {
            return BadRequest(new { message = "Password reset failed." });
        }

        return NoContent();
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] UserRoleUpdateDto roleUpdateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _userService.UpdateUserRolesAsync(id, roleUpdateDto.Roles);
        if (!success)
        {
            return NotFound(new { message = $"User with ID {id} was not found or role update failed." });
        }

        return NoContent();
    }
}
