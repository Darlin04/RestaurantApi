using Microsoft.EntityFrameworkCore;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Api.IntegrationTests.Common;

[TestFixture]
public sealed class MigrationsTests
{
    [Test]
    public async Task AllMigrations_ApplyOnEmptyDatabase()
    {
        // Arrange
        var databaseName = $"migrations_{Guid.NewGuid():N}".ToLowerInvariant();
        var baseConnection = PostgresFixture.Container.GetConnectionString();
        var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = baseConnection };
        builder["Database"] = databaseName;
        var connectionString = builder.ConnectionString;

        using (var admin = new Npgsql.NpgsqlConnection(baseConnection))
        {
            await admin.OpenAsync();
            using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await create.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseNpgsql(connectionString, o => o.MigrationsAssembly(typeof(RestaurantDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        // Act
        await using var db = new RestaurantDbContext(options);
        await db.Database.MigrateAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();

        // Assert
        Assert.That(pending, Is.Empty);
    }
}
