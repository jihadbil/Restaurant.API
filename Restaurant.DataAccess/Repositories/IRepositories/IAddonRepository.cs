using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IAddonRepository : IGenericRepository<Addon>
{
    void Update(Addon addon);
}
