using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound(new { message = $"Category with ID {id} was not found." });
        }
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryCreateDto categoryCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdCategory = await _categoryService.CreateCategoryAsync(categoryCreateDto);
        return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.Id }, createdCategory);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryUpdateDto)
    {
        if (id != categoryUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _categoryService.UpdateCategoryAsync(categoryUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"Category with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var success = await _categoryService.DeleteCategoryAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Category with ID {id} was not found." });
        }

        return NoContent();
    }
}
