using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Ingredients;

[TestFixture]
public sealed class QueryIngredientsTests
{
    [Test]
    public async Task List_TwoIngredients_ReturnsThemOrderedByName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var queso = Ingredient.Create("Queso");
        var ajo = Ingredient.Create("Ajo");
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().AddRange(queso, ajo);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await waiter.GetAsync("/api/v1/ingredients");
        var body = await response.Content.ReadFromJsonAsync<List<IngredientPayload>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var ours = body!.Where(i => i.Id == queso.Id || i.Id == ajo.Id).Select(i => i.Name).ToList();
        Assert.That(ours, Is.EqualTo(new[] { "Ajo", "Queso" }));
    }

    [Test]
    public async Task Get_ExistingIngredient_Returns200()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_query_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var ingredient = Ingredient.Create("Tomate");
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(ingredient);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await admin.GetAsync($"/api/v1/ingredients/{ingredient.Id}");
        var body = await response.Content.ReadFromJsonAsync<IngredientPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Name, Is.EqualTo("Tomate"));
    }

    [Test]
    public async Task Get_UnknownIngredient_Returns404WithIngredientNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.GetAsync($"/api/v1/ingredients/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(document.RootElement.GetProperty("code").GetString(), Is.EqualTo("Ingredient.NotFound"));
    }

    [Test]
    public async Task List_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_query_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.GetAsync("/api/v1/ingredients");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private sealed record IngredientPayload(Guid Id, string Name);
}
