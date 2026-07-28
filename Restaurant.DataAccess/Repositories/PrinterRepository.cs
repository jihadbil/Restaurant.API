using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class PrinterRepository : GenericRepository<Printer>, IPrinterRepository
{
    public PrinterRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(Printer printer)
    {
        _db.Printers.Update(printer);
    }
}
