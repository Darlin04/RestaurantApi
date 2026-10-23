using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Tables;

namespace Restaurant.Api.IntegrationTests.Tables;

[TestFixture]
public sealed class QueryTablesTests
{
    [Test]
    public async Task List_FilteredByStatus_ReturnsOnlyAvailableOrderedByNumber()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_query_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var first = TestNumbers.NextTableNumber();
        var second = TestNumbers.NextTableNumber();
        var inService = TestNumbers.NextTableNumber();
        var ids = new Dictionary<int, Guid>();

        await factory.ExecuteDbContextAsync(async db =>
        {
            var tableA = Table.Create(second, 4, null);
            var tableB = Table.Create(first, 2, null);
            var tableC = Table.Create(inService, 6, null);
            tableC.StartService();
            db.Set<Table>().AddRange(tableA, tableB, tableC);
            await db.SaveChangesAsync();
            ids[second] = tableA.Id;
            ids[first] = tableB.Id;
            ids[inService] = tableC.Id;
        });

        // Act
        var response = await admin.GetAsync("/api/v1/tables?status=Available");
        var body = await response.Content.ReadFromJsonAsync<List<TablePayload>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var ours = body!.Where(t => ids.Values.Contains(t.Id)).ToList();
        Assert.That(ours.Select(t => t.Id).ToList(), Is.EqualTo(new[] { ids[first], ids[second] }));
        Assert.That(body!.All(t => t.Status == "Available"), Is.True);
    }

    [Test]
    public async Task List_InvalidStatus_Returns400()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.GetAsync("/api/v1/tables?status=Ocupada");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Get_ExistingTable_Returns200()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, "Terraza");
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Table>().Add(table);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await waiter.GetAsync($"/api/v1/tables/{table.Id}");
        var body = await response.Content.ReadFromJsonAsync<TablePayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Number, Is.EqualTo(table.Number));
    }

    [Test]
    public async Task Get_UnknownTable_Returns404WithTableNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.GetAsync($"/api/v1/tables/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(document.RootElement.GetProperty("code").GetString(), Is.EqualTo("Table.NotFound"));
    }

    [Test]
    public async Task Get_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_query_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.GetAsync("/api/v1/tables");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private sealed record TablePayload(Guid Id, int Number, int Capacity, string? Description, string Status);
}
