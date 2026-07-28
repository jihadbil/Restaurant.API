using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IOrderItemRepository : IGenericRepository<OrderItem>
{
    void Update(OrderItem orderItem);
}
