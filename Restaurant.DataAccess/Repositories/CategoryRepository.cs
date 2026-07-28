using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(Category category)
    {
        _db.Categories.Update(category);
    }
}
