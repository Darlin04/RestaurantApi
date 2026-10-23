using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Tables;

[TestFixture]
public sealed class CreateTableTests
{
    [Test]
    public async Task Create_ValidTable_Returns201WithAvailableStatus()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var number = TestNumbers.NextTableNumber();

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/tables", new { number, capacity = 4, description = "Terraza" });
        var body = await response.Content.ReadFromJsonAsync<TablePayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Number, Is.EqualTo(number));
        Assert.That(body.Status, Is.EqualTo("Available"));
        Assert.That(response.Headers.Location?.ToString(), Does.Contain($"/api/v1/tables/{body.Id}"));
    }

    [Test]
    public async Task Create_DuplicateNumber_Returns409()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var number = TestNumbers.NextTableNumber();
        await admin.PostAsJsonAsync("/api/v1/tables", new { number, capacity = 4, description = (string?)null });

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/tables", new { number, capacity = 2, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Table.DuplicateNumber"));
    }

    [TestCase(0)]
    [TestCase(21)]
    public async Task Create_CapacityOutOfRange_Returns400(int capacity)
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/tables", new { number = TestNumbers.NextTableNumber(), capacity, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("capacity"));
    }

    [Test]
    public async Task Create_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_create_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/tables", new { number = TestNumbers.NextTableNumber(), capacity = 4, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Create_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "tbl_create_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.PostAsJsonAsync("/api/v1/tables", new { number = TestNumbers.NextTableNumber(), capacity = 4, description = (string?)null });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record TablePayload(Guid Id, int Number, int Capacity, string? Description, string Status);
}
