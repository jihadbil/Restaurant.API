using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto> CreateProductAsync(ProductCreateDto productCreateDto);
    Task<bool> UpdateProductAsync(ProductUpdateDto productUpdateDto);
    Task<bool> DeleteProductAsync(int id);
}
