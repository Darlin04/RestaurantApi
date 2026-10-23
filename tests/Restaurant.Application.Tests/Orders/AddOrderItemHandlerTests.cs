using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders.AddOrderItem;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Orders;

[TestFixture]
public sealed class AddOrderItemHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task HandleAsync_ExistingDish_SumsQuantityAndSaves()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(2)], Now);
        var fixture = new Fixture(order, dish, table);

        // Act
        var result = await fixture.Handler.HandleAsync(new AddOrderItemCommand(order.Id, dish.Id, 1), CancellationToken.None);

        // Assert
        result.Items.Single().Quantity.ShouldBe(3);
        result.Subtotal.ShouldBe(1350m);
        result.TableNumber.ShouldBe(3);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_UnknownOrder_ThrowsOrderNotFound()
    {
        // Arrange
        var fixture = new Fixture(null, null, null);

        // Act
        var act = () => fixture.Handler.HandleAsync(new AddOrderItemCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), 1), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Order.NotFound");
    }

    [Test]
    public async Task HandleAsync_UnknownDish_ThrowsDishNotFoundAndDoesNotSave()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(1)], Now);
        var fixture = new Fixture(order, null, table);

        // Act
        var act = () => fixture.Handler.HandleAsync(new AddOrderItemCommand(order.Id, Guid.CreateVersion7(), 1), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Dish.NotFound");
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_CompletedOrder_ThrowsNotInProgress()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(1)], Now);
        order.Complete(Now);
        var fixture = new Fixture(order, dish, table);

        // Act
        var act = () => fixture.Handler.HandleAsync(new AddOrderItemCommand(order.Id, dish.Id, 1), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Order.NotInProgress");
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture
    {
        public Fixture(Order? order, Dish? dish, Table? table)
        {
            var orders = new Mock<IOrderRepository>();
            orders.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var dishes = new Mock<IDishRepository>();
            dishes.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(dish);

            var tables = new Mock<ITableRepository>();
            tables.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(table);

            UnitOfWork = new Mock<IUnitOfWork>();
            Handler = new AddOrderItemHandler(orders.Object, dishes.Object, tables.Object, UnitOfWork.Object);
        }

        public AddOrderItemHandler Handler { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; }
    }
}
