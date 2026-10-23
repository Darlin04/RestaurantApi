using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Tables;

namespace Restaurant.Api.IntegrationTests.Tables;

[TestFixture]
public sealed class ReleaseTableTests
{
    [Test]
    public async Task Release_ServedTable_Returns200Available()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_release_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = await InsertTableAsync(factory, TableStatus.Served);

        // Act
        var response = await waiter.PatchAsync($"/api/v1/tables/{table.Id}/release", null);
        var body = await response.Content.ReadFromJsonAsync<TablePayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Status, Is.EqualTo("Available"));
    }

    [Test]
    public async Task Release_InServiceTable_Returns422InvalidTransition()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_release_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = await InsertTableAsync(factory, TableStatus.InService);

        // Act
        var response = await waiter.PatchAsync($"/api/v1/tables/{table.Id}/release", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Table.InvalidTransition"));
    }

    [Test]
    public async Task Release_UnknownTable_Returns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_release_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PatchAsync($"/api/v1/tables/{Guid.NewGuid()}/release", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Release_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_release_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.PatchAsync($"/api/v1/tables/{Guid.NewGuid()}/release", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static async Task<Table> InsertTableAsync(ApiFactory factory, TableStatus status)
    {
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, null);
        if (status is TableStatus.InService or TableStatus.Served)
        {
            table.StartService();
        }

        if (status == TableStatus.Served)
        {
            table.MarkServed();
        }

        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Table>().Add(table);
            return db.SaveChangesAsync();
        });

        return table;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record TablePayload(Guid Id, int Number, int Capacity, string? Description, string Status);
}
