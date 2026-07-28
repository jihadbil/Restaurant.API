using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class CashboxRepository : GenericRepository<Cashbox>, ICashboxRepository
{
    public CashboxRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(Cashbox cashbox)
    {
        _db.Cashboxes.Update(cashbox);
    }
}
