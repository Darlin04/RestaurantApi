using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders.OpenOrder;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Orders;

[TestFixture]
public sealed class OpenOrderHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task HandleAsync_ValidOrder_OpensOrderAndStartsTableService()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var waiterId = Guid.CreateVersion7();
        var fixture = new Fixture(table, [dish], waiterId);

        // Act
        var result = await fixture.Handler.HandleAsync(
            new OpenOrderCommand(table.Id, [new OpenOrderItem(dish.Id, 2)]),
            CancellationToken.None);

        // Assert
        result.TableNumber.ShouldBe(3);
        result.Status.ShouldBe("InProgress");
        result.Subtotal.ShouldBe(900m);
        result.WaiterId.ShouldBe(waiterId);
        table.Status.ShouldBe(TableStatus.InService);
        fixture.Orders.Verify(x => x.Add(It.IsAny<Order>()), Times.Once);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_UnknownTable_ThrowsTableNotFoundBeforeLoadingDishes()
    {
        // Arrange
        var fixture = new Fixture(null, [], Guid.CreateVersion7());

        // Act
        var act = () => fixture.Handler.HandleAsync(
            new OpenOrderCommand(Guid.CreateVersion7(), [new OpenOrderItem(Guid.CreateVersion7(), 1)]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Table.NotFound");
        fixture.Dishes.Verify(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_MissingDish_ThrowsDishNotFoundAndLeavesTableAvailable()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var fixture = new Fixture(table, [], Guid.CreateVersion7());

        // Act
        var act = () => fixture.Handler.HandleAsync(
            new OpenOrderCommand(table.Id, [new OpenOrderItem(Guid.CreateVersion7(), 1)]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Dish.NotFound");
        table.Status.ShouldBe(TableStatus.Available);
        fixture.Orders.Verify(x => x.Add(It.IsAny<Order>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_InactiveDish_ThrowsInactiveAndLeavesTableAvailable()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        dish.Deactivate();
        var fixture = new Fixture(table, [dish], Guid.CreateVersion7());

        // Act
        var act = () => fixture.Handler.HandleAsync(
            new OpenOrderCommand(table.Id, [new OpenOrderItem(dish.Id, 1)]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Dish.Inactive");
        table.Status.ShouldBe(TableStatus.Available);
        fixture.Orders.Verify(x => x.Add(It.IsAny<Order>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_TableInService_ThrowsInvalidTransitionAndDoesNotAdd()
    {
        // Arrange
        var table = Table.Create(3, 4, null);
        table.StartService();
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var fixture = new Fixture(table, [dish], Guid.CreateVersion7());

        // Act
        var act = () => fixture.Handler.HandleAsync(
            new OpenOrderCommand(table.Id, [new OpenOrderItem(dish.Id, 1)]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Table.InvalidTransition");
        fixture.Orders.Verify(x => x.Add(It.IsAny<Order>()), Times.Never);
        fixture.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Fixture
    {
        public Fixture(Table? table, IReadOnlyList<Dish> dishList, Guid waiterId)
        {
            var tables = new Mock<ITableRepository>();
            tables.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(table);

            Dishes = new Mock<IDishRepository>();
            Dishes.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dishList);

            Orders = new Mock<IOrderRepository>();
            UnitOfWork = new Mock<IUnitOfWork>();

            var currentUser = new Mock<ICurrentUser>();
            currentUser.SetupGet(x => x.UserId).Returns(waiterId);
            currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);

            Handler = new OpenOrderHandler(
                tables.Object,
                Dishes.Object,
                Orders.Object,
                currentUser.Object,
                new FixedTimeProvider(Now),
                UnitOfWork.Object);
        }

        public OpenOrderHandler Handler { get; }

        public Mock<IDishRepository> Dishes { get; }

        public Mock<IOrderRepository> Orders { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; }
    }
}
