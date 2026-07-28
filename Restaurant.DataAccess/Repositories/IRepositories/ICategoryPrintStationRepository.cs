using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface ICategoryPrintStationRepository : IGenericRepository<CategoryPrintStation>
{
    void Update(CategoryPrintStation categoryPrintStation);
}
