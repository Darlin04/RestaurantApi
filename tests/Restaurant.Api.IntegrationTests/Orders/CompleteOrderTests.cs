using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Api.IntegrationTests.Orders;

[TestFixture]
public sealed class CompleteOrderTests
{
    [Test]
    public async Task Complete_InProgressOrder_Returns200AndMarksTableServed()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_complete_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var dish = await InsertDishAsync(factory, 450m);
        var order = await InsertOrderAsync(factory, waiterId, table, dish);

        // Act
        var response = await waiter.PatchAsync($"/api/v1/orders/{order.Id}/complete", null);
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();
        var tableAfter = await waiter.GetFromJsonAsync<TablePayload>($"/api/v1/tables/{table.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Status, Is.EqualTo("Completed"));
        Assert.That(body.CompletedAt, Is.Not.Null);
        Assert.That(tableAfter!.Status, Is.EqualTo("Served"));
    }

    [Test]
    public async Task Complete_AlreadyCompleted_Returns422NotInProgress()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_complete_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await waiter.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = await InsertTableAsync(factory);
        var dish = await InsertDishAsync(factory, 450m);
        var order = await InsertOrderAsync(factory, waiterId, table, dish);
        await waiter.PatchAsync($"/api/v1/orders/{order.Id}/complete", null);

        // Act
        var response = await waiter.PatchAsync($"/api/v1/orders/{order.Id}/complete", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Order.NotInProgress"));
    }

    [Test]
    public async Task Complete_UnknownOrder_Returns404()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_complete_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PatchAsync($"/api/v1/orders/{Guid.NewGuid()}/complete", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("Order.NotFound"));
    }

    [Test]
    public async Task Complete_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_complete_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.PatchAsync($"/api/v1/orders/{Guid.NewGuid()}/complete", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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

    private static async Task<Order> InsertOrderAsync(ApiFactory factory, Guid waiterId, Table table, Dish dish)
    {
        var order = Order.Open(table.Id, waiterId, [dish.ToOrderLine(1)], DateTimeOffset.UtcNow);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Order>().Add(order);
            return db.SaveChangesAsync();
        });

        return order;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record MePayload(Guid Id);

    private sealed record TablePayload(Guid Id, int Number, string Status);

    private sealed record OrderPayload(Guid Id, string Status, DateTimeOffset? CompletedAt);
}
