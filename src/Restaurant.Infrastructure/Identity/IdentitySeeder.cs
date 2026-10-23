using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Identity;

public sealed class IdentitySeeder(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(services, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var configuration = provider.GetRequiredService<IConfiguration>();
        var dbOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        var db = provider.GetRequiredService<RestaurantDbContext>();
        if (dbOptions.ApplyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        var roles = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { "Admin", "Waiter" })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new InvalidOperationException("Falta la configuración 'Seed:AdminEmail'. Configúrala con dotnet user-secrets o variables de entorno.");
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Falta la configuración 'Seed:AdminPassword'. Configúrala con dotnet user-secrets o variables de entorno.");
        }

        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await users.FindByEmailAsync(adminEmail);
        if (existing is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Sistema",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await users.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario administrador: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await users.AddToRoleAsync(admin, "Admin");
        }
    }
}
