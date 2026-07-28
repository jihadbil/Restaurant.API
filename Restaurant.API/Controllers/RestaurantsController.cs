using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantsController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestaurantDto>>> GetRestaurants()
    {
        var restaurants = await _restaurantService.GetAllRestaurantsAsync();
        return Ok(restaurants);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantDto>> GetRestaurant(int id)
    {
        var restaurant = await _restaurantService.GetRestaurantByIdAsync(id);
        if (restaurant == null)
        {
            return NotFound(new { message = $"Restaurant with ID {id} was not found." });
        }
        return Ok(restaurant);
    }

    [HttpPost]
    public async Task<ActionResult<RestaurantDto>> CreateRestaurant([FromBody] RestaurantCreateDto restaurantCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var createdRestaurant = await _restaurantService.CreateRestaurantAsync(restaurantCreateDto, userId);
        return CreatedAtAction(nameof(GetRestaurant), new { id = createdRestaurant.Id }, createdRestaurant);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRestaurant(int id, [FromBody] RestaurantUpdateDto restaurantUpdateDto)
    {
        if (id != restaurantUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _restaurantService.UpdateRestaurantAsync(restaurantUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"Restaurant with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        var success = await _restaurantService.DeleteRestaurantAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Restaurant with ID {id} was not found." });
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
        var uploadsFolder = Path.Combine(webRootPath, "images", "restaurants");

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

        var relativeUrl = $"/images/restaurants/{fileName}";
        return Ok(new { imageUrl = relativeUrl });
    }
}
