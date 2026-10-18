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
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _environment;

    private ProductDto ForViewer(ProductDto product)
    {
        product.CanViewCosts = Restaurant.Models.AppPermissions.Has(User, Restaurant.Models.AppPermissions.Reports)
            || Restaurant.Models.AppPermissions.Has(User, Restaurant.Models.AppPermissions.ManageProducts)
            || Restaurant.Models.AppPermissions.Has(User, Restaurant.Models.AppPermissions.Dashboard);
        if (!product.CanViewCosts) product.CostPrice = 0;
        return product;
    }

    public ProductsController(IProductService productService, IWebHostEnvironment environment)
    {
        _productService = productService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products.Select(ForViewer));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }
        return Ok(ForViewer(product));
    }

    [HttpPost]
    [Authorize(Policy = "Permission.Products.Manage")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] ProductCreateDto productCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdProduct = await _productService.CreateProductAsync(productCreateDto);
        return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Permission.Products.Manage")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto productUpdateDto)
    {
        if (id != productUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _productService.UpdateProductAsync(productUpdateDto);
        if (!success)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Permission.Products.Manage")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var success = await _productService.DeleteProductAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpPost("upload-image")]
    [Authorize(Policy = "Permission.Products.Manage")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var result = await ImageUpload.SaveAsync(file, _environment.WebRootPath, "products");
        return result.Error == null
            ? Ok(new { imageUrl = result.Url })
            : BadRequest(new { message = result.Error });
    }
}
