using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    void Update(Category category);
}
