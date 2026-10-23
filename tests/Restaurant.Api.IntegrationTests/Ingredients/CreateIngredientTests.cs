using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Ingredients;

[TestFixture]
public sealed class CreateIngredientTests
{
    [Test]
    public async Task Create_ValidName_Returns201WithTrimmedName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/ingredients", new { name = "  Tomate  " });
        var body = await response.Content.ReadFromJsonAsync<IngredientPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Name, Is.EqualTo("Tomate"));
        Assert.That(response.Headers.Location?.ToString(), Does.Contain($"/api/v1/ingredients/{body.Id}"));
    }

    [Test]
    public async Task Create_DuplicateNameIgnoringCase_Returns409()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        await admin.PostAsJsonAsync("/api/v1/ingredients", new { name = "Tomate" });

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/ingredients", new { name = "TOMATE" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Ingredient.DuplicateName"));
    }

    [Test]
    public async Task Create_OneCharacterName_Returns422InvalidName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/ingredients", new { name = "A" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Ingredient.InvalidName"));
    }

    [Test]
    public async Task Create_EmptyName_Returns400()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/ingredients", new { name = "" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_create_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/ingredients", new { name = "Tomate" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Create_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_create_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.PostAsJsonAsync("/api/v1/ingredients", new { name = "Tomate" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record IngredientPayload(Guid Id, string Name);
}
