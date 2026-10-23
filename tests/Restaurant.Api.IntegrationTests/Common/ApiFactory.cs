using System.Data.Common;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Api.IntegrationTests.Common;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public ApiFactory(string? databaseName = null)
    {
        _databaseName = databaseName ?? "restaurant_tests";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var containerConnection = PostgresFixture.Container.GetConnectionString();
        var connectionString = WithDatabase(containerConnection, _databaseName);

        EnsureDatabaseExists(containerConnection, _databaseName);

        var jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Jwt:Key"] = jwtKey,
                ["Seed:AdminEmail"] = $"admin-{Guid.NewGuid():N}@test.local",
                ["Seed:AdminPassword"] = $"Test-Passw0rd!{Guid.NewGuid():N}",
                ["RateLimiting:AuthPermitLimit"] = "1000",
                ["Database:ApplyMigrationsOnStartup"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            var hosted = services.Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType != null &&
                    d.ImplementationType.Name.Contains("IdentitySeeder", StringComparison.Ordinal))
                .ToList();

            foreach (var descriptor in hosted)
            {
                services.Remove(descriptor);
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        db.Database.EnsureCreated();
        Restaurant.Infrastructure.Identity.IdentitySeeder.SeedAsync(host.Services).GetAwaiter().GetResult();
        return host;
    }

    public async Task ExecuteDbContextAsync(Func<RestaurantDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        await action(db);
    }

    private static string WithDatabase(string connectionString, string database)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        builder["Database"] = database;
        return builder.ConnectionString;
    }

    private static void EnsureDatabaseExists(string connectionString, string database)
    {
        if (database == "restaurant_tests")
        {
            // Shared database: ensure it exists once.
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{database.ToLowerInvariant()}'";
                var exists = command.ExecuteScalar();
                if (exists is null)
                {
                    using var create = connection.CreateCommand();
                    create.CommandText = $"CREATE DATABASE \"{database.ToLowerInvariant()}\"";
                    create.ExecuteNonQuery();
                }
            }
            catch
            {
                // Container may already have the database; EnsureCreated will handle the schema.
            }

            return;
        }

        using var adminConnection = new Npgsql.NpgsqlConnection(connectionString);
        adminConnection.Open();
        using var createCommand = adminConnection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{database}\"";
        try
        {
            createCommand.ExecuteNonQuery();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P04")
        {
            // Database already exists.
        }
    }
}
