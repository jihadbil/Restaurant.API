using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync(includeProperties: "Category");
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _unitOfWork.Products.GetFirstOrDefaultAsync(p => p.Id == id, includeProperties: "Category");
        return _mapper.Map<ProductDto?>(product);
    }

    public async Task<ProductDto> CreateProductAsync(ProductCreateDto productCreateDto)
    {
        var product = _mapper.Map<Product>(productCreateDto);
        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveAsync();
        
        // Re-fetch to include the category name
        var savedProduct = await _unitOfWork.Products.GetFirstOrDefaultAsync(p => p.Id == product.Id, includeProperties: "Category");
        return _mapper.Map<ProductDto>(savedProduct);
    }

    public async Task<bool> UpdateProductAsync(ProductUpdateDto productUpdateDto)
    {
        var product = await _unitOfWork.Products.GetFirstOrDefaultAsync(p => p.Id == productUpdateDto.Id, tracked: false);
        if (product == null)
        {
            return false;
        }

        _mapper.Map(productUpdateDto, product);
        _unitOfWork.Products.Update(product);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _unitOfWork.Products.GetFirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return false;
        }

        _unitOfWork.Products.Remove(product);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
