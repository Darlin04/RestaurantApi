using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Dishes;

[TestFixture]
public sealed class CreateDishTests
{
    [Test]
    public async Task Create_ValidDish_Returns201WithIngredientsOrderedByName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = await InsertIngredientAsync(factory, "Pollo");
        var arroz = await InsertIngredientAsync(factory, "Arroz");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/dishes", new
        {
            name = "Locrio " + Guid.NewGuid().ToString("N")[..8],
            price = 450.00m,
            servings = 1,
            category = "MainCourse",
            ingredientIds = new[] { pollo.Id, arroz.Id },
        });
        var body = await response.Content.ReadFromJsonAsync<DishPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.IsActive, Is.True);
        Assert.That(body.Ingredients.Select(i => i.Name).ToList(), Is.EqualTo(new[] { "Arroz", "Pollo" }));
    }

    [Test]
    public async Task Create_UnknownIngredient_Returns404WithIngredientNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var unknownId = Guid.NewGuid();

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/dishes", new
        {
            name = "Locrio " + Guid.NewGuid().ToString("N")[..8],
            price = 450.00m,
            servings = 1,
            category = "MainCourse",
            ingredientIds = new[] { unknownId },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Ingredient.NotFound"));
    }

    [Test]
    public async Task Create_DuplicateName_Returns409DishDuplicateName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = await InsertIngredientAsync(factory, "Pollo");
        var payload = new
        {
            name = "Locrio " + Guid.NewGuid().ToString("N")[..8],
            price = 450.00m,
            servings = 1,
            category = "MainCourse",
            ingredientIds = new[] { pollo.Id },
        };
        await admin.PostAsJsonAsync("/api/v1/dishes", payload);

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/dishes", payload);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Dish.DuplicateName"));
    }

    [Test]
    public async Task Create_PriceWithThreeDecimals_Returns422TooManyDecimals()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_create_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = await InsertIngredientAsync(factory, "Pollo");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/dishes", new
        {
            name = "Locrio " + Guid.NewGuid().ToString("N")[..8],
            price = 450.001m,
            servings = 1,
            category = "MainCourse",
            ingredientIds = new[] { pollo.Id },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Money.TooManyDecimals"));
    }

    [Test]
    public async Task Create_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_create_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/dishes", new
        {
            name = "Locrio",
            price = 450.00m,
            servings = 1,
            category = "MainCourse",
            ingredientIds = new[] { Guid.NewGuid() },
        });

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

    private sealed record DishPayload(Guid Id, string Name, decimal Price, int Servings, string Category, bool IsActive, List<IngredientPayload> Ingredients);

    private sealed record IngredientPayload(Guid Id, string Name);
}
