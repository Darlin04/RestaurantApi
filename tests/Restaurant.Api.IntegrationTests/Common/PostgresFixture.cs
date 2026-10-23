using Testcontainers.PostgreSql;

namespace Restaurant.Api.IntegrationTests.Common;

[SetUpFixture]
public sealed class PostgresFixture
{
    public static PostgreSqlContainer Container { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        Container = new PostgreSqlBuilder("postgres:17")
            .Build();

        await Container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}
