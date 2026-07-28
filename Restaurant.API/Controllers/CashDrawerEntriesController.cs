using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CashDrawerEntriesController : ControllerBase
{
    private readonly ICashDrawerEntryService _entryService;

    public CashDrawerEntriesController(ICashDrawerEntryService entryService)
    {
        _entryService = entryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CashDrawerEntryDto>>> GetEntries(
        [FromQuery] int? cashboxId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var entries = await _entryService.GetAllEntriesAsync(cashboxId, from, to);
        return Ok(entries);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CashDrawerEntryDto>> GetEntry(int id)
    {
        var entry = await _entryService.GetEntryByIdAsync(id);
        if (entry == null)
        {
            return NotFound(new { message = $"Cash drawer entry with ID {id} was not found." });
        }
        return Ok(entry);
    }

    [HttpGet("byorder/{orderId}")]
    public async Task<ActionResult<IEnumerable<CashDrawerEntryDto>>> GetEntriesByOrder(int orderId)
    {
        var entries = await _entryService.GetEntriesByOrderAsync(orderId);
        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<CashDrawerEntryDto>> CreateEntry([FromBody] CashDrawerEntryCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var createdEntry = await _entryService.CreateEntryAsync(dto);
            return CreatedAtAction(nameof(GetEntry), new { id = createdEntry.Id }, createdEntry);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var success = await _entryService.DeleteEntryAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Cash drawer entry with ID {id} was not found." });
        }

        return NoContent();
    }
}
