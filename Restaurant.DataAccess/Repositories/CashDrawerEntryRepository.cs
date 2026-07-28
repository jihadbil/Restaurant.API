using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class CashDrawerEntryRepository : GenericRepository<CashDrawerEntry>, ICashDrawerEntryRepository
{
    public CashDrawerEntryRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(CashDrawerEntry cashDrawerEntry)
    {
        _db.CashDrawerEntries.Update(cashDrawerEntry);
    }
}
