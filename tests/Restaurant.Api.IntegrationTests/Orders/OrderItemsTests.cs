using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Api.IntegrationTests.Orders;

[TestFixture]
public sealed class OrderItemsTests
{
    [Test]
    public async Task AddItem_ExistingDish_Returns200WithSummedQuantityAndSubtotal()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_items_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var locrio = await InsertDishAsync(factory, 450m);
        var order = await InsertOrderAsync(factory, waiterId, table, [locrio.ToOrderLine(2)]);

        // Act
        var response = await waiter.PostAsJsonAsync($"/api/v1/orders/{order.Id}/items", new { dishId = locrio.Id, quantity = 1 });
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Items.Single().Quantity, Is.EqualTo(3));
        Assert.That(body.Subtotal, Is.EqualTo(1350m));
    }

    [Test]
    public async Task AddItem_DishPriceChanged_CreatesSecondItemAtNewPrice()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_items_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var locrio = await InsertDishAsync(factory, 450m);
        var order = await InsertOrderAsync(factory, waiterId, table, [locrio.ToOrderLine(2)]);
        await ChangeDishPriceAsync(factory, locrio.Id, 500m);

        // Act
        var response = await waiter.PostAsJsonAsync($"/api/v1/orders/{order.Id}/items", new { dishId = locrio.Id, quantity = 1 });
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Items.Count, Is.EqualTo(2));
        Assert.That(body.Subtotal, Is.EqualTo(1400m));
    }

    [Test]
    public async Task RemoveItem_OneOfTwo_Returns200WithOneItem()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_items_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var locrio = await InsertDishAsync(factory, 450m);
        var flan = await InsertDishAsync(factory, 100m);
        var order = await InsertOrderAsync(factory, waiterId, table, [locrio.ToOrderLine(1), flan.ToOrderLine(1)]);
        var itemId = order.Items.First().Id;

        // Act
        var response = await waiter.DeleteAsync($"/api/v1/orders/{order.Id}/items/{itemId}");
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveItem_LastItem_Returns422CannotRemoveLastItem()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_items_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var locrio = await InsertDishAsync(factory, 450m);
        var order = await InsertOrderAsync(factory, waiterId, table, [locrio.ToOrderLine(1)]);

        // Act
        var response = await waiter.DeleteAsync($"/api/v1/orders/{order.Id}/items/{order.Items.Single().Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Order.CannotRemoveLastItem"));
    }

    [Test]
    public async Task AddItem_CompletedOrder_Returns422NotInProgress()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_items_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var locrio = await InsertDishAsync(factory, 450m);
        var order = await InsertOrderAsync(factory, waiterId, table, [locrio.ToOrderLine(1)]);
        await CompleteOrderAsync(factory, order.Id);

        // Act
        var response = await waiter.PostAsJsonAsync($"/api/v1/orders/{order.Id}/items", new { dishId = locrio.Id, quantity = 1 });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Order.NotInProgress"));
    }

    [Test]
    public async Task AddItem_AdminCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_items_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync($"/api/v1/orders/{Guid.NewGuid()}/items", new { dishId = Guid.NewGuid(), quantity = 1 });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task<Table> InsertTableAsync(ApiFactory factory)
    {
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, null);
        table.StartService();
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Table>().Add(table);
            return db.SaveChangesAsync();
        });

        return table;
    }

    private static async Task<Dish> InsertDishAsync(ApiFactory factory, decimal price)
    {
        var pollo = Ingredient.Create("Pollo " + Guid.NewGuid().ToString("N")[..8]);
        var dish = Dish.Create("Plato " + Guid.NewGuid().ToString("N")[..8], Money.Create(price), 1, DishCategory.MainCourse, [pollo.Id]);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Ingredient>().Add(pollo);
            db.Set<Dish>().Add(dish);
            return db.SaveChangesAsync();
        });

        return dish;
    }

    private static async Task<Order> InsertOrderAsync(ApiFactory factory, Guid waiterId, Table table, IReadOnlyList<OrderLine> lines)
    {
        var order = Order.Open(table.Id, waiterId, lines, DateTimeOffset.UtcNow);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Order>().Add(order);
            return db.SaveChangesAsync();
        });

        return order;
    }

    private static async Task CompleteOrderAsync(ApiFactory factory, Guid orderId) =>
        await factory.ExecuteDbContextAsync(async db =>
        {
            var order = await db.Set<Order>().FirstAsync(o => o.Id == orderId);
            order.Complete(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        });

    private static async Task ChangeDishPriceAsync(ApiFactory factory, Guid dishId, decimal price) =>
        await factory.ExecuteDbContextAsync(async db =>
        {
            var dish = await db.Set<Dish>().FirstAsync(d => d.Id == dishId);
            dish.Update(dish.Name.Value, Money.Create(price), dish.Servings, dish.Category, dish.Ingredients.Select(i => i.IngredientId).ToList());
            await db.SaveChangesAsync();
        });

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record MePayload(Guid Id);

    private sealed record OrderPayload(Guid Id, decimal Subtotal, List<ItemPayload> Items);

    private sealed record ItemPayload(Guid Id, int Quantity);
}
