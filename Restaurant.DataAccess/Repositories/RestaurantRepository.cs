using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class RestaurantRepository : GenericRepository<RestaurantInfo>, IRestaurantRepository
{
    public RestaurantRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(RestaurantInfo restaurant)
    {
        _db.Restaurants.Update(restaurant);
    }
}
