using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(OrderItem orderItem)
    {
        _db.OrderItems.Update(orderItem);
    }
}
