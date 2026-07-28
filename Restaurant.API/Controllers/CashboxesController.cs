using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CashboxesController : ControllerBase
{
    private readonly ICashboxService _cashboxService;

    public CashboxesController(ICashboxService cashboxService)
    {
        _cashboxService = cashboxService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CashboxDto>>> GetCashboxes()
    {
        var cashboxes = await _cashboxService.GetAllCashboxesAsync();
        return Ok(cashboxes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CashboxDto>> GetCashbox(int id)
    {
        var cashbox = await _cashboxService.GetCashboxByIdAsync(id);
        if (cashbox == null)
        {
            return NotFound(new { message = $"Cashbox with ID {id} was not found." });
        }
        return Ok(cashbox);
    }

    [HttpGet("{id}/balance")]
    public async Task<ActionResult<CashboxBalanceDto>> GetCashboxBalance(int id)
    {
        var balance = await _cashboxService.GetCashboxBalanceAsync(id);
        if (balance == null)
        {
            return NotFound(new { message = $"Cashbox with ID {id} was not found." });
        }
        return Ok(balance);
    }

    [HttpPost]
    public async Task<ActionResult<CashboxDto>> CreateCashbox([FromBody] CashboxCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdCashbox = await _cashboxService.CreateCashboxAsync(dto);
        return CreatedAtAction(nameof(GetCashbox), new { id = createdCashbox.Id }, createdCashbox);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCashbox(int id, [FromBody] CashboxUpdateDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _cashboxService.UpdateCashboxAsync(dto);
        if (!success)
        {
            return NotFound(new { message = $"Cashbox with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCashbox(int id)
    {
        var success = await _cashboxService.DeleteCashboxAsync(id);
        if (!success)
        {
            return BadRequest(new { message = $"Could not delete Cashbox with ID {id}. Make sure it exists and does not contain any entries." });
        }

        return NoContent();
    }
}
