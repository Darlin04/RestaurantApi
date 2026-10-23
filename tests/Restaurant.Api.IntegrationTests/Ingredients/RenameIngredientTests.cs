using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Ingredients;

[TestFixture]
public sealed class RenameIngredientTests
{
    [Test]
    public async Task Rename_ToNewName_Returns200WithNewName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_rename_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var ingredient = await InsertIngredientAsync(factory, "Tomate");

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/ingredients/{ingredient.Id}", new { name = "Tomate cherry" });
        var body = await response.Content.ReadFromJsonAsync<IngredientPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Name, Is.EqualTo("Tomate cherry"));
    }

    [Test]
    public async Task Rename_SameNameDifferentCase_Returns200()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_rename_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var ingredient = await InsertIngredientAsync(factory, "Tomate");

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/ingredients/{ingredient.Id}", new { name = "TOMATE" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Rename_NameOfAnotherIngredient_Returns409()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_rename_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var tomate = await InsertIngredientAsync(factory, "Tomate");
        await InsertIngredientAsync(factory, "Queso");

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/ingredients/{tomate.Id}", new { name = "queso" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Ingredient.DuplicateName"));
    }

    [Test]
    public async Task Rename_UnknownIngredient_Returns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_rename_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/ingredients/{Guid.NewGuid()}", new { name = "Queso" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Ingredient.NotFound"));
    }

    [Test]
    public async Task Rename_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_rename_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var ingredient = await InsertIngredientAsync(factory, "Tomate");

        // Act
        var response = await waiter.PutAsJsonAsync($"/api/v1/ingredients/{ingredient.Id}", new { name = "Queso" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task<Ingredient> InsertIngredientAsync(ApiFactory factory, string name)
    {
        var ingredient = Ingredient.Create(name);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(ingredient);
            return db.SaveChangesAsync();
        });

        return ingredient;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record IngredientPayload(Guid Id, string Name);
}
