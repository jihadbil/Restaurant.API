using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly IWebHostEnvironment _environment;

    public PaymentMethodsController(IPaymentMethodService paymentMethodService, IWebHostEnvironment environment)
    {
        _paymentMethodService = paymentMethodService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodDto>>> GetPaymentMethods()
    {
        var methods = await _paymentMethodService.GetAllPaymentMethodsAsync();
        return Ok(methods);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentMethodDto>> GetPaymentMethod(int id)
    {
        var method = await _paymentMethodService.GetPaymentMethodByIdAsync(id);
        if (method == null)
        {
            return NotFound(new { message = $"PaymentMethod with ID {id} was not found." });
        }
        return Ok(method);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentMethodDto>> CreatePaymentMethod([FromBody] PaymentMethodCreateDto paymentMethodCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdMethod = await _paymentMethodService.CreatePaymentMethodAsync(paymentMethodCreateDto);
        return CreatedAtAction(nameof(GetPaymentMethod), new { id = createdMethod.Id }, createdMethod);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePaymentMethod(int id, [FromBody] PaymentMethodUpdateDto paymentMethodUpdateDto)
    {
        if (id != paymentMethodUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _paymentMethodService.UpdatePaymentMethodAsync(paymentMethodUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"PaymentMethod with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePaymentMethod(int id)
    {
        var success = await _paymentMethodService.DeletePaymentMethodAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"PaymentMethod with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("upload-logo")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var result = await ImageUpload.SaveAsync(file, _environment.WebRootPath, "payment_methods");
        return result.Error == null
            ? Ok(new { imageUrl = result.Url })
            : BadRequest(new { message = result.Error });
    }
}
