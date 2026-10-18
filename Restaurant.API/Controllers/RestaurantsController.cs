using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;
    private readonly IWebHostEnvironment _environment;

    public RestaurantsController(IRestaurantService restaurantService, IWebHostEnvironment environment)
    {
        _restaurantService = restaurantService;
        _environment = environment;
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

    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult<RestaurantDto>> GetCurrentRestaurant()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var restaurant = await _restaurantService.GetCurrentRestaurantAsync(userId);
        return restaurant == null
            ? NotFound(new { message = "لم يتم تحديد المطعم لهذا المستخدم. راجع بيانات المطعم في الإعدادات." })
            : Ok(restaurant);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var result = await ImageUpload.SaveAsync(file, _environment.WebRootPath, "restaurants");
        return result.Error == null
            ? Ok(new { imageUrl = result.Url })
            : BadRequest(new { message = result.Error });
    }
}
