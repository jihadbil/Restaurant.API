using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IPrinterService _printerService;

    public PrintersController(IPrinterService printerService)
    {
        _printerService = printerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrinterDto>>> GetPrinters()
    {
        var printers = await _printerService.GetAllPrintersAsync();
        return Ok(printers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PrinterDto>> GetPrinter(int id)
    {
        var printer = await _printerService.GetPrinterByIdAsync(id);
        if (printer == null)
        {
            return NotFound(new { message = $"Printer with ID {id} was not found." });
        }
        return Ok(printer);
    }

    [HttpPost]
    public async Task<ActionResult<PrinterDto>> CreatePrinter([FromBody] PrinterCreateDto printerCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdPrinter = await _printerService.CreatePrinterAsync(printerCreateDto);
        return CreatedAtAction(nameof(GetPrinter), new { id = createdPrinter.Id }, createdPrinter);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePrinter(int id, [FromBody] PrinterUpdateDto printerUpdateDto)
    {
        if (id != printerUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _printerService.UpdatePrinterAsync(printerUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"Printer with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrinter(int id)
    {
        var success = await _printerService.DeletePrinterAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Printer with ID {id} was not found." });
        }

        return NoContent();
    }
}
