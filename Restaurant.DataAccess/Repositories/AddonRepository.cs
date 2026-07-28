using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class AddonRepository : GenericRepository<Addon>, IAddonRepository
{
    public AddonRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(Addon addon)
    {
        _db.Addons.Update(addon);
    }
}
