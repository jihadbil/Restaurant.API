using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintStationsController : ControllerBase
{
    private readonly IPrintStationService _printStationService;

    public PrintStationsController(IPrintStationService printStationService)
    {
        _printStationService = printStationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrintStationDto>>> GetPrintStations()
    {
        var stations = await _printStationService.GetAllPrintStationsAsync();
        return Ok(stations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PrintStationDto>> GetPrintStation(int id)
    {
        var station = await _printStationService.GetPrintStationByIdAsync(id);
        if (station == null)
        {
            return NotFound(new { message = $"PrintStation with ID {id} was not found." });
        }
        return Ok(station);
    }

    [HttpPost]
    public async Task<ActionResult<PrintStationDto>> CreatePrintStation([FromBody] PrintStationCreateDto printStationCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdStation = await _printStationService.CreatePrintStationAsync(printStationCreateDto);
        return CreatedAtAction(nameof(GetPrintStation), new { id = createdStation.Id }, createdStation);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePrintStation(int id, [FromBody] PrintStationUpdateDto printStationUpdateDto)
    {
        if (id != printStationUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _printStationService.UpdatePrintStationAsync(printStationUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"PrintStation with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrintStation(int id)
    {
        var success = await _printStationService.DeletePrintStationAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"PrintStation with ID {id} was not found." });
        }

        return NoContent();
    }
}
