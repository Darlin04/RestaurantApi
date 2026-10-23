using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders.CompleteOrder;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Orders;

[TestFixture]
public sealed class CompleteOrderHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task HandleAsync_InProgressOrder_CompletesOrderAndMarksTableServed()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        table.StartService();
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(1)], Now);
        var fixture = new Fixture(order, table, Now.AddMinutes(20));

        // Act
        var result = await fixture.Handler.HandleAsync(new CompleteOrderCommand(order.Id), CancellationToken.None);

        // Assert
        result.Status.ShouldBe("Completed");
        result.CompletedAt.ShouldBe(Now.AddMinutes(20));
        table.Status.ShouldBe(TableStatus.Served);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AlreadyCompleted_ThrowsNotInProgressAndDoesNotSave()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        table.StartService();
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var order = Order.Open(table.Id, Guid.CreateVersion7(), [dish.ToOrderLine(1)], Now);
        order.Complete(Now);
        var fixture = new Fixture(order, table, Now);

        // Act
        var act = () => fixture.Handler.HandleAsync(new CompleteOrderCommand(order.Id), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Order.NotInProgress");
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_UnknownOrder_ThrowsOrderNotFound()
    {
        // Arrange
        var fixture = new Fixture(null, null, Now);

        // Act
        var act = () => fixture.Handler.HandleAsync(new CompleteOrderCommand(Guid.CreateVersion7()), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Order.NotFound");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Fixture
    {
        public Fixture(Order? order, Table? table, DateTimeOffset now)
        {
            var orders = new Mock<IOrderRepository>();
            orders.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var tables = new Mock<ITableRepository>();
            tables.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(table);

            UnitOfWork = new Mock<IUnitOfWork>();
            Handler = new CompleteOrderHandler(orders.Object, tables.Object, new FixedTimeProvider(now), UnitOfWork.Object);
        }

        public CompleteOrderHandler Handler { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; }
    }
}
