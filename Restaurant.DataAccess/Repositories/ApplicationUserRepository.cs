using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class ApplicationUserRepository : GenericRepository<ApplicationUser>, IApplicationUserRepository
{
    public ApplicationUserRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(ApplicationUser user)
    {
        _db.Users.Update(user);
    }
}
