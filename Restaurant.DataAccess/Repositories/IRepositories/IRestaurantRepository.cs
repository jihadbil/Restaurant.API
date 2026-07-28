using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IRestaurantRepository : IGenericRepository<RestaurantInfo>
{
    void Update(RestaurantInfo restaurant);
}
