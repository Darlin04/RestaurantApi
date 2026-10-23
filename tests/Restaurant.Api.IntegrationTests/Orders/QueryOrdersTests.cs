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
public sealed class QueryOrdersTests
{
    [Test]
    public async Task List_TwentyFiveCompletedOrders_SecondPageOfTenIsOrderedByCreatedAtDesc()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_query_" + Guid.NewGuid().ToString("N"));
        var staff = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await staff.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, null);
        var dish = await InsertDishAsync(factory, 450m);
        var now = DateTimeOffset.UtcNow;
        var orders = Enumerable.Range(0, 25).Select(i =>
        {
            var order = Order.Open(table.Id, waiterId, [dish.ToOrderLine(1)], now.AddMinutes(-i));
            order.Complete(now.AddMinutes(-i));
            return order;
        }).ToList();
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Table>().Add(table);
            db.Set<Order>().AddRange(orders);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await staff.GetAsync($"/api/v1/orders?tableId={table.Id}&page=2&pageSize=10");
        var body = await response.Content.ReadFromJsonAsync<PagePayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Items.Count, Is.EqualTo(10));
        Assert.That(body.TotalCount, Is.EqualTo(25));
        Assert.That(body.TotalPages, Is.EqualTo(3));
        var createdAt = body.Items.Select(i => i.CreatedAt).ToList();
        Assert.That(createdAt, Is.Ordered.Descending);
    }

    [Test]
    public async Task List_PageSizeAboveOneHundred_Returns400()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_query_" + Guid.NewGuid().ToString("N"));
        var staff = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await staff.GetAsync("/api/v1/orders?pageSize=101");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Get_ExistingOrder_Returns200WithItems()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_query_" + Guid.NewGuid().ToString("N"));
        var staff = await TestAuth.CreateClientAsync(factory, "Waiter");
        var waiterId = (await staff.GetFromJsonAsync<MePayload>("/api/v1/auth/me"))!.Id;
        var table = Table.Create(TestNumbers.NextTableNumber(), 4, null);
        var dish = await InsertDishAsync(factory, 450m);
        var order = Order.Open(table.Id, waiterId, [dish.ToOrderLine(2)], DateTimeOffset.UtcNow);
        await factory.ExecuteDbContextAsync(db =>
        {
            db.Set<Table>().Add(table);
            db.Set<Order>().Add(order);
            return db.SaveChangesAsync();
        });

        // Act
        var response = await staff.GetAsync($"/api/v1/orders/{order.Id}");
        var body = await response.Content.ReadFromJsonAsync<OrderPayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Items.Single().Quantity, Is.EqualTo(2));
        Assert.That(body.Subtotal, Is.EqualTo(900m));
    }

    [Test]
    public async Task Get_UnknownOrder_Returns404OrderNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_query_" + Guid.NewGuid().ToString("N"));
        var staff = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await staff.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(document.RootElement.GetProperty("code").GetString(), Is.EqualTo("Order.NotFound"));
    }

    [Test]
    public async Task List_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "ord_query_" + Guid.NewGuid().ToString("N"));
        var anonymous = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await anonymous.GetAsync("/api/v1/orders");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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

    private sealed record MePayload(Guid Id);

    private sealed record PagePayload(List<SummaryPayload> Items, int TotalCount, int TotalPages);

    private sealed record SummaryPayload(Guid Id, DateTimeOffset CreatedAt);

    private sealed record OrderPayload(Guid Id, decimal Subtotal, List<ItemPayload> Items);

    private sealed record ItemPayload(Guid Id, int Quantity);
}
