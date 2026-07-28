using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;

    public PaymentMethodsController(IPaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
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
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Only JPG, JPEG, and PNG images are allowed." });
        }

        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "images", "payment_methods");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/images/payment_methods/{fileName}";
        return Ok(new { imageUrl = relativeUrl });
    }
}
