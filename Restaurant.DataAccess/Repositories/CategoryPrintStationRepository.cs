using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class CategoryPrintStationRepository : GenericRepository<CategoryPrintStation>, ICategoryPrintStationRepository
{
    public CategoryPrintStationRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(CategoryPrintStation categoryPrintStation)
    {
        _db.CategoryPrintStations.Update(categoryPrintStation);
    }
}
