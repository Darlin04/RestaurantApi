using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders.RemoveOrderItem;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Orders;

[TestFixture]
public sealed class RemoveOrderItemHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task HandleAsync_OneOfTwoItems_RemovesItemAndSaves()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var locrio = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var flan = Dish.Create("Flan", Money.Create(100m), 1, DishCategory.Dessert, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [locrio.ToOrderLine(2), flan.ToOrderLine(1)], Now);
        var fixture = new Fixture(order, table);

        // Act
        var result = await fixture.Handler.HandleAsync(
            new RemoveOrderItemCommand(order.Id, order.Items.First().Id),
            CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Subtotal.ShouldBe(100m);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_LastItem_ThrowsCannotRemoveLastItemAndDoesNotSave()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(1)], Now);
        var fixture = new Fixture(order, table);

        // Act
        var act = () => fixture.Handler.HandleAsync(
            new RemoveOrderItemCommand(order.Id, order.Items.Single().Id),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Order.CannotRemoveLastItem");
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_UnknownOrder_ThrowsOrderNotFound()
    {
        // Arrange
        var fixture = new Fixture(null, null);

        // Act
        var act = () => fixture.Handler.HandleAsync(new RemoveOrderItemCommand(Guid.CreateVersion7(), Guid.CreateVersion7()), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Order.NotFound");
    }

    [Test]
    public async Task HandleAsync_UnknownItem_ThrowsItemNotFound()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(1), dish.ToOrderLine(1)], Now);
        var fixture = new Fixture(order, table);

        // Act
        var act = () => fixture.Handler.HandleAsync(new RemoveOrderItemCommand(order.Id, Guid.CreateVersion7()), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Order.ItemNotFound");
    }

    private sealed class Fixture
    {
        public Fixture(Order? order, Table? table)
        {
            var orders = new Mock<IOrderRepository>();
            orders.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var tables = new Mock<ITableRepository>();
            tables.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(table);

            UnitOfWork = new Mock<IUnitOfWork>();
            Handler = new RemoveOrderItemHandler(orders.Object, tables.Object, UnitOfWork.Object);
        }

        public RemoveOrderItemHandler Handler { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; }
    }
}
