using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    void Update(Order order);
}
