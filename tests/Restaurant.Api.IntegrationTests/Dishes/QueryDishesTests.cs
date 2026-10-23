using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Dishes;

[TestFixture]
public sealed class QueryDishesTests
{
    [Test]
    public async Task List_FilteredByCategoryAndActive_ReturnsOnlyMatchingDishesWithIngredients()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var crema = Ingredient.Create("Crema");
        var leche = Ingredient.Create("Leche");
        var active = Dish.Create("Flan " + Guid.NewGuid().ToString("N")[..8], Domain.Common.Money.Create(120m), 2, DishCategory.Dessert, [crema.Id, leche.Id]);
        var inactive = Dish.Create("Flan inactivo " + Guid.NewGuid().ToString("N")[..8], Domain.Common.Money.Create(120m), 2, DishCategory.Dessert, [crema.Id]);
        inactive.Deactivate();
        var mainCourse = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], Domain.Common.Money.Create(450m), 1, DishCategory.MainCourse, [leche.Id]);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().AddRange(crema, leche);
            db.Set<Dish>().AddRange(active, inactive, mainCourse);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await waiter.GetAsync("/api/v1/dishes?category=Dessert&isActive=true");
        var body = await response.Content.ReadFromJsonAsync<List<DishPayload>>() ?? [];

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body.Select(d => d.Id).ToList(), Does.Contain(active.Id));
        Assert.That(body.Select(d => d.Id).ToList(), Does.Not.Contain(inactive.Id));
        Assert.That(body.Select(d => d.Id).ToList(), Does.Not.Contain(mainCourse.Id));
        var item = body.Single(d => d.Id == active.Id);
        Assert.That(item.Ingredients.Select(i => i.Name).ToList(), Is.EqualTo(new[] { "Crema", "Leche" }));
    }

    [Test]
    public async Task Get_ExistingDish_Returns200WithIngredients()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var leche = Ingredient.Create("Leche");
        var dish = Dish.Create("Flan " + Guid.NewGuid().ToString("N")[..8], Domain.Common.Money.Create(120m), 2, DishCategory.Dessert, [leche.Id]);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(leche);
            db.Set<Dish>().Add(dish);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await waiter.GetAsync($"/api/v1/dishes/{dish.Id}");
        var body = await response.Content.ReadFromJsonAsync<DishPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Ingredients.Single().Id, Is.EqualTo(leche.Id));
    }

    [Test]
    public async Task Get_UnknownDish_Returns404WithDishNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_query_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.GetAsync($"/api/v1/dishes/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(document.RootElement.GetProperty("code").GetString(), Is.EqualTo("Dish.NotFound"));
    }

    [Test]
    public async Task List_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_query_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.GetAsync("/api/v1/dishes");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private sealed record DishPayload(Guid Id, string Name, List<IngredientPayload> Ingredients);

    private sealed record IngredientPayload(Guid Id, string Name);
}
