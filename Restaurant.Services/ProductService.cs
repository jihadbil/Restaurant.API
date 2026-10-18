using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        Money.Validate(productCreateDto.SalePrice, false);
        Money.Validate(productCreateDto.CostPrice, false);
        if (!await _unitOfWork.Categories.Query().AnyAsync(c => c.Id == productCreateDto.CategoryId))
            throw new ArgumentException("التصنيف المحدد غير موجود.");
        var product = _mapper.Map<Product>(productCreateDto);
        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveAsync();
        
        // Re-fetch to include the category name
        var savedProduct = await _unitOfWork.Products.GetFirstOrDefaultAsync(p => p.Id == product.Id, includeProperties: "Category");
        return _mapper.Map<ProductDto>(savedProduct);
    }

    public async Task<bool> UpdateProductAsync(ProductUpdateDto productUpdateDto)
    {
        Money.Validate(productUpdateDto.SalePrice, false);
        Money.Validate(productUpdateDto.CostPrice, false);
        if (!await _unitOfWork.Categories.Query().AnyAsync(c => c.Id == productUpdateDto.CategoryId))
            throw new ArgumentException("التصنيف المحدد غير موجود.");
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

        if (await _unitOfWork.OrderItems.Query().AnyAsync(i => i.ProductId == id))
            throw new InvalidOperationException("لا يمكن حذف صنف مستخدم في فواتير. يمكنك إيقاف توفره بدلاً من ذلك.");
        _unitOfWork.Products.Remove(product);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
