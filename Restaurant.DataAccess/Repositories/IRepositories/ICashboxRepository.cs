using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface ICashboxRepository : IGenericRepository<Cashbox>
{
    void Update(Cashbox cashbox);
}
