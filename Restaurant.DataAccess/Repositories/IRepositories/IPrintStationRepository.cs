using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IPrintStationRepository : IGenericRepository<PrintStation>
{
    void Update(PrintStation printStation);
}
