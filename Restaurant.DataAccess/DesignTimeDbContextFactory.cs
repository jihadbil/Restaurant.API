using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Restaurant.DataAccess;

// EF tooling must never execute application startup or seed a live database.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RestaurantDbContext>
{
    public RestaurantDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<RestaurantDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(local);Database=RestaurantDb;Integrated Security=true;TrustServerCertificate=true").Options);
}
