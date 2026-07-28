using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(Product product)
    {
        _db.Products.Update(product);
    }
}
