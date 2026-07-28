using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(Order order)
    {
        _db.Orders.Update(order);
    }
}
