using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Restaurant.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RestaurantDbContext>
{
    public RestaurantDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("RESTAURANT_DESIGN_CONNECTION")
            ?? "Host=localhost;Database=restaurant_design";

        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseNpgsql(connectionString, o => o.MigrationsAssembly(typeof(RestaurantDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new RestaurantDbContext(options);
    }
}
