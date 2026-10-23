using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;
using Restaurant.Domain.Tables;

namespace Restaurant.Api.IntegrationTests.Orders;

[TestFixture]
public sealed class OpenOrderTests
{
    [Test]
    public async Task Open_AvailableTable_Returns201AndPutsTableInService()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_open_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var me = await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me");
        var table = await InsertTableAsync(factory, inService: false);
        var dish = await InsertDishAsync(factory, Money.Create(450m), active: true);

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            tableId = table.Id,
            items = new[] { new { dishId = dish.Id, quantity = 2 } },
        });
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();
        var tableAfter = await waiter.GetFromJsonAsync<TablePayload>($"/api/v1/tables/{table.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Status, Is.EqualTo("InProgress"));
        Assert.That(body.Subtotal, Is.EqualTo(900m));
        Assert.That(body.TableNumber, Is.EqualTo(table.Number));
        Assert.That(body.WaiterId, Is.EqualTo(me!.Id));
        Assert.That(tableAfter!.Status, Is.EqualTo("InService"));
    }

    [Test]
    public async Task Open_RepeatedDish_CombinesIntoOneItem()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_open_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = await InsertTableAsync(factory, inService: false);
        var dish = await InsertDishAsync(factory, Money.Create(450m), active: true);

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            tableId = table.Id,
            items = new[] { new { dishId = dish.Id, quantity = 1 }, new { dishId = dish.Id, quantity = 2 } },
        });
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Items.Count, Is.EqualTo(1));
        Assert.That(body.Items.Single().Quantity, Is.EqualTo(3));
    }

    [Test]
    public async Task Open_TableInService_Returns422InvalidTransition()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_open_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = await InsertTableAsync(factory, inService: true);
        var dish = await InsertDishAsync(factory, Money.Create(450m), active: true);

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            tableId = table.Id,
            items = new[] { new { dishId = dish.Id, quantity = 1 } },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Table.InvalidTransition"));
    }

    [Test]
    public async Task Open_InactiveDish_Returns422DishInactive()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_open_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var table = await InsertTableAsync(factory, inService: false);
        var dish = await InsertDishAsync(factory, Money.Create(450m), active: false);

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            tableId = table.Id,
            items = new[] { new { dishId = dish.Id, quantity = 1 } },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Dish.Inactive"));
    }

    [Test]
    public async Task Open_UnknownTable_Returns404TableNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_open_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var dish = await InsertDishAsync(factory, Money.Create(450m), active: true);

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            tableId = Guid.NewGuid(),
            items = new[] { new { dishId = dish.Id, quantity = 1 } },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Table.NotFound"));
    }

    [Test]
    public async Task Open_AdminCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_open_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/orders", new
        {
            tableId = Guid.NewGuid(),
            items = new[] { new { dishId = Guid.NewGuid(), quantity = 1 } },
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task<Table> InsertTableAsync(ApiFactory factory, bool inService)
    {
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, null);
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

    private static async Task<Dish> InsertDishAsync(ApiFactory factory, Money price, bool active)
    {
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio " + Guid.NewGuid().ToString("N")[..8], price, 1, DishCategory.MainCourse, [pollo.Id]);
        if (!active)
        {
            dish.Deactivate();
        }

        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(pollo);
            db.Set<Dish>().Add(dish);
            return db.SaveChangesAsync();
        });

        return dish;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record MePayload(Guid Id);

    private sealed record TablePayload(Guid Id, int Number, string Status);

    private sealed record OrderPayload(Guid Id, Guid WaiterId, int TableNumber, string Status, decimal Subtotal, List<ItemPayload> Items);

    private sealed record ItemPayload(Guid Id, int Quantity);
}
