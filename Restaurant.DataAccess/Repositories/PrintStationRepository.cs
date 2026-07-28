using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class PrintStationRepository : GenericRepository<PrintStation>, IPrintStationRepository
{
    public PrintStationRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(PrintStation printStation)
    {
        _db.PrintStations.Update(printStation);
    }
}
