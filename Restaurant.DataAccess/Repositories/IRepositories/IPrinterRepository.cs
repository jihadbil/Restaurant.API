using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IPrinterRepository : IGenericRepository<Printer>
{
    void Update(Printer printer);
}
