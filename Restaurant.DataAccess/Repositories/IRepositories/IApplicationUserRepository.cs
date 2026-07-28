using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IApplicationUserRepository : IGenericRepository<ApplicationUser>
{
    void Update(ApplicationUser user);
}
