using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IProductRepository : IGenericRepository<Product>
{
    void Update(Product product);
}
