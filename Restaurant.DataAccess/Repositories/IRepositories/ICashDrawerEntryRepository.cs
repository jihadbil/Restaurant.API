using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface ICashDrawerEntryRepository : IGenericRepository<CashDrawerEntry>
{
    void Update(CashDrawerEntry cashDrawerEntry);
}
