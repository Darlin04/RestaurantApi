using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Tables;

namespace Restaurant.Api.IntegrationTests.Tables;

[TestFixture]
public sealed class UpdateTableTests
{
    [Test]
    public async Task Update_AvailableTable_Returns200AndNullsEmptyDescription()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var table = await InsertTableAsync(factory, inService: false);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/tables/{table.Id}", new { capacity = 6, description = "" });
        var body = await response.Content.ReadFromJsonAsync<TablePayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Capacity, Is.EqualTo(6));
        Assert.That(body.Description, Is.Null);
    }

    [Test]
    public async Task Update_InServiceTable_Returns422NotAvailable()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var table = await InsertTableAsync(factory, inService: true);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/tables/{table.Id}", new { capacity = 6, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Table.NotAvailable"));
    }

    [Test]
    public async Task Update_UnknownTable_Returns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/tables/{Guid.NewGuid()}", new { capacity = 6, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Table.NotFound"));
    }

    [Test]
    public async Task Update_CapacityZero_Returns400()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var table = await InsertTableAsync(factory, inService: false);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/tables/{table.Id}", new { capacity = 0, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Update_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_update_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = await InsertTableAsync(factory, inService: false);

        // Act
        var response = await waiter.PutAsJsonAsync($"/api/v1/tables/{table.Id}", new { capacity = 6, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task<Table> InsertTableAsync(ApiFactory factory, bool inService)
    {
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, "Terraza");
        if (inService)
        {
            table.StartService();
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
