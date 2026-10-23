using System.Net;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Ingredients;

[TestFixture]
public sealed class DeleteIngredientTests
{
    [Test]
    public async Task Delete_UnusedIngredient_Returns204AndGetReturns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_delete_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var ajo = Ingredient.Create("Ajo");
        await InsertAsync(factory, ajo);

        // Act
        var response = await admin.DeleteAsync($"/api/v1/ingredients/{ajo.Id}");
        var get = await admin.GetAsync($"/api/v1/ingredients/{ajo.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_IngredientUsedByDish_Returns409InUse()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_delete_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(pollo);
            db.Set<Dish>().Add(dish);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await admin.DeleteAsync($"/api/v1/ingredients/{pollo.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(document.RootElement.GetProperty("code").GetString(), Is.EqualTo("Ingredient.InUse"));
    }

    [Test]
    public async Task Delete_UnknownIngredient_Returns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_delete_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.DeleteAsync($"/api/v1/ingredients/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ing_delete_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.DeleteAsync($"/api/v1/ingredients/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task InsertAsync(ApiFactory factory, Ingredient ingredient)
    {
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(ingredient);
            return db.SaveChangesAsync();
        });
    }
}
