using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Dishes;

[TestFixture]
public sealed class UpdateDishTests
{
    [Test]
    public async Task Update_NewPriceAndIngredients_Returns200WithNewData()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = Ingredient.Create("Pollo");
        var arroz = Ingredient.Create("Arroz");
        var dish = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        await InsertAsync(factory, pollo, arroz, dish);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/dishes/{dish.Id}", new
        {
            name = dish.Name.Value,
            price = 500.00m,
            servings = 2,
            category = "MainCourse",
            ingredientIds = new[] { arroz.Id },
        });
        var body = await response.Content.ReadFromJsonAsync<DishPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Price, Is.EqualTo(500m));
        Assert.That(body.Ingredients.Select(i => i.Id).ToList(), Is.EqualTo(new[] { arroz.Id }));
    }

    [Test]
    public async Task Update_InactiveDish_Returns422Inactive()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        dish.Deactivate();
        await InsertAsync(factory, pollo, null, dish);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/dishes/{dish.Id}", new
        {
            name = dish.Name.Value,
            price = 500.00m,
            servings = 2,
            category = "MainCourse",
            ingredientIds = new[] { pollo.Id },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Dish.Inactive"));
    }

    [Test]
    public async Task Update_NameOfAnotherDish_Returns409DishDuplicateName()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_update_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = Ingredient.Create("Pollo");
        var otherName = "Flan " + Guid.NewGuid().ToString("N")[..8];
        var other = Dish.Create(otherName, Money.Create(120m), 1, DishCategory.Dessert, [pollo.Id]);
        var dish = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        await InsertAsync(factory, pollo, null, dish, other);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/v1/dishes/{dish.Id}", new
        {
            name = otherName.ToLowerInvariant(),
            price = 450.00m,
            servings = 1,
            category = "MainCourse",
            ingredientIds = new[] { pollo.Id },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Dish.DuplicateName"));
    }

    [Test]
    public async Task Update_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_update_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PutAsJsonAsync($"/api/v1/dishes/{Guid.NewGuid()}", new
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

    private static async Task InsertAsync(ApiFactory factory, Ingredient first, Ingredient? second, params Dish[] dishes)
    {
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(first);
            if (second is not null)
            {
                db.Set<Ingredient>().Add(second);
            }

            db.Set<Dish>().AddRange(dishes);
            return db.SaveChangesAsync();
        });
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record DishPayload(Guid Id, string Name, decimal Price, int Servings, string Category, bool IsActive, List<IngredientPayload> Ingredients);

    private sealed record IngredientPayload(Guid Id, string Name);
}
