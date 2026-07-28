using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddonsController : ControllerBase
{
    private readonly IAddonService _addonService;

    public AddonsController(IAddonService addonService)
    {
        _addonService = addonService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AddonDto>>> GetAddons()
    {
        var addons = await _addonService.GetAllAddonsAsync();
        return Ok(addons);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AddonDto>> GetAddon(int id)
    {
        var addon = await _addonService.GetAddonByIdAsync(id);
        if (addon == null)
        {
            return NotFound(new { message = $"Addon with ID {id} was not found." });
        }
        return Ok(addon);
    }

    [HttpPost]
    public async Task<ActionResult<AddonDto>> CreateAddon([FromBody] AddonCreateDto addonCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdAddon = await _addonService.CreateAddonAsync(addonCreateDto);
        return CreatedAtAction(nameof(GetAddon), new { id = createdAddon.Id }, createdAddon);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddon(int id, [FromBody] AddonUpdateDto addonUpdateDto)
    {
        if (id != addonUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _addonService.UpdateAddonAsync(addonUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"Addon with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddon(int id)
    {
        var success = await _addonService.DeleteAddonAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Addon with ID {id} was not found." });
        }

        return NoContent();
    }
}
