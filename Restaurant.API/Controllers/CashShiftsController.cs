using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Security.Claims;

namespace Restaurant.API.Controllers;

[ApiController, Authorize, Route("api/cashshifts")]
public class CashShiftsController(ICashShiftService shifts) : ControllerBase
{
    private bool CanViewTotals => TreasuryAuthorization.CanViewTotals(User);

    [HttpGet("context/{cashboxId}"), Authorize(Policy = "ShiftAccess")]
    public async Task<ActionResult<CashShiftContextDto>> Context(int cashboxId)
    {
        try { return Ok(await shifts.GetContextAsync(cashboxId, CanViewTotals)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("close"), Authorize(Policy = "ShiftAccess")]
    public async Task<ActionResult<CashShiftDto>> Close(CashShiftCloseDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        try { return Ok(await shifts.CloseAsync(dto, userId, CanViewTotals)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Conflict(new { message = "تعارضت عملية الإغلاق مع عملية أخرى. حدّث البيانات ثم أعد المحاولة." }); }
    }

    [HttpGet, Authorize(Policy = "TreasuryAccess")]
    public async Task<ActionResult<IEnumerable<CashShiftDto>>> History([FromQuery] int? cashboxId) =>
        Ok(await shifts.GetHistoryAsync(cashboxId));

    [HttpGet("byrequest/{requestId}"), Authorize(Policy = "ShiftAccess")]
    public async Task<ActionResult<CashShiftDto>> ByRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var saved = await shifts.GetByRequestAsync(requestId, userId, CanViewTotals);
        return saved == null ? NotFound() : Ok(saved);
    }
}
