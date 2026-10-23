using System.Net;
using System.Net.Http.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Api.IntegrationTests.Dishes;

[TestFixture]
public sealed class DishAvailabilityTests
{
    [Test]
    public async Task Deactivate_ActiveDish_Returns204AndGetShowsInactive()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_avail_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var dish = await InsertDishAsync(factory);

        // Act
        var response = await admin.PatchAsync($"/api/v1/dishes/{dish.Id}/deactivate", null);
        var get = await admin.GetFromJsonAsync<DishPayload>($"/api/v1/dishes/{dish.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(get!.IsActive, Is.False);
    }

    [Test]
    public async Task Deactivate_TwiceInARow_BothReturn204()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_avail_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var dish = await InsertDishAsync(factory);

        // Act
        var first = await admin.PatchAsync($"/api/v1/dishes/{dish.Id}/deactivate", null);
        var second = await admin.PatchAsync($"/api/v1/dishes/{dish.Id}/deactivate", null);

        // Assert
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Activate_InactiveDish_Returns204AndGetShowsActive()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_avail_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var dish = await InsertDishAsync(factory);
        await admin.PatchAsync($"/api/v1/dishes/{dish.Id}/deactivate", null);

        // Act
        var response = await admin.PatchAsync($"/api/v1/dishes/{dish.Id}/activate", null);
        var get = await admin.GetFromJsonAsync<DishPayload>($"/api/v1/dishes/{dish.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(get!.IsActive, Is.True);
    }

    [Test]
    public async Task Deactivate_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_avail_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var dish = await InsertDishAsync(factory);

        // Act
        var response = await waiter.PatchAsync($"/api/v1/dishes/{dish.Id}/deactivate", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Deactivate_UnknownDish_Returns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "dsh_avail_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PatchAsync($"/api/v1/dishes/{Guid.NewGuid()}/deactivate", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static async Task<Dish> InsertDishAsync(ApiFactory factory)
    {
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(pollo);
            db.Set<Dish>().Add(dish);
            return db.SaveChangesAsync();
        });

        return dish;
    }

    private sealed record DishPayload(Guid Id, string Name, bool IsActive);
}
