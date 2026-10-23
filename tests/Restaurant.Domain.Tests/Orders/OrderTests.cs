using Restaurant.Domain.Common;
using Restaurant.Domain.Orders;

using Shouldly;

namespace Restaurant.Domain.Tests.Orders;

[TestFixture]
public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Open_OneLine_ComputesSubtotalAndStartsInProgress()
    {
        // Arrange
        var dishId = Guid.CreateVersion7();
        var lines = new[] { Line(dishId, 450m, 2) };

        // Act
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), lines, Now);

        // Assert
        order.Status.ShouldBe(OrderStatus.InProgress);
        order.Subtotal.ShouldBe(Money.Create(900m));
        order.Items.Count.ShouldBe(1);
        order.CreatedAt.ShouldBe(Now);
        order.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Open_RepeatedDish_CombinesQuantities()
    {
        // Arrange
        var dishId = Guid.CreateVersion7();
        var lines = new[] { Line(dishId, 450m, 1), Line(dishId, 450m, 2) };

        // Act
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), lines, Now);

        // Assert
        order.Items.Count.ShouldBe(1);
        order.Items.Single().Quantity.ShouldBe(3);
    }

    [Test]
    public void Open_NoLines_ThrowsItemsRequired()
    {
        // Arrange
        // Act
        var act = () => Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [], Now);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.ItemsRequired");
    }

    [Test]
    public void Open_QuantityAboveFifty_ThrowsInvalidQuantity()
    {
        // Arrange
        var lines = new[] { Line(Guid.CreateVersion7(), 10m, 51) };

        // Act
        var act = () => Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), lines, Now);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.InvalidQuantity");
    }

    [Test]
    public void Open_RepeatedDishAboveFiftyInTotal_ThrowsInvalidQuantity()
    {
        // Arrange
        var dishId = Guid.CreateVersion7();
        var lines = new[] { Line(dishId, 10m, 30), Line(dishId, 10m, 21) };

        // Act
        var act = () => Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), lines, Now);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.InvalidQuantity");
    }

    [Test]
    public void AddItem_SameDishAndPrice_SumsQuantityAndRecalculatesSubtotal()
    {
        // Arrange
        var dishId = Guid.CreateVersion7();
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(dishId, 450m, 2)], Now);

        // Act
        order.AddItem(Line(dishId, 450m, 1));

        // Assert
        order.Items.Single().Quantity.ShouldBe(3);
        order.Subtotal.ShouldBe(Money.Create(1350m));
    }

    [Test]
    public void AddItem_SameDishDifferentPrice_CreatesSecondItem()
    {
        // Arrange
        var dishId = Guid.CreateVersion7();
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(dishId, 450m, 2)], Now);

        // Act
        order.AddItem(Line(dishId, 500m, 1));

        // Assert
        order.Items.Count.ShouldBe(2);
        order.Subtotal.ShouldBe(Money.Create(1400m));
    }

    [Test]
    public void AddItem_CompletedOrder_ThrowsNotInProgress()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 10m, 1)], Now);
        order.Complete(Now);

        // Act
        var act = () => order.AddItem(Line(Guid.CreateVersion7(), 10m, 1));

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.NotInProgress");
    }

    [Test]
    public void AddItem_ExistingItemPushedAboveFifty_ThrowsInvalidQuantityAndKeepsQuantity()
    {
        // Arrange
        var dishId = Guid.CreateVersion7();
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(dishId, 10m, 45)], Now);

        // Act
        var act = () => order.AddItem(Line(dishId, 10m, 6));

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.InvalidQuantity");
        order.Items.Single().Quantity.ShouldBe(45);
    }

    [Test]
    public void RemoveItem_OneOfTwo_RemovesItemAndRecalculatesSubtotal()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 450m, 2), Line(Guid.CreateVersion7(), 100m, 1)], Now);
        var toRemove = order.Items.First();

        // Act
        order.RemoveItem(toRemove.Id);

        // Assert
        order.Items.Count.ShouldBe(1);
        order.Subtotal.ShouldBe(Money.Create(100m));
    }

    [Test]
    public void RemoveItem_LastItem_ThrowsCannotRemoveLastItem()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 450m, 1)], Now);

        // Act
        var act = () => order.RemoveItem(order.Items.Single().Id);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.CannotRemoveLastItem");
    }

    [Test]
    public void RemoveItem_UnknownItem_ThrowsItemNotFound()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 450m, 1), Line(Guid.CreateVersion7(), 100m, 1)], Now);

        // Act
        var act = () => order.RemoveItem(Guid.CreateVersion7());

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.ItemNotFound");
    }

    [Test]
    public void RemoveItem_CompletedOrder_ThrowsNotInProgress()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 450m, 1), Line(Guid.CreateVersion7(), 100m, 1)], Now);
        order.Complete(Now);

        // Act
        var act = () => order.RemoveItem(order.Items.First().Id);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.NotInProgress");
    }

    [Test]
    public void Complete_InProgressOrder_SetsCompletedAt()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 450m, 1)], Now);
        var later = Now.AddMinutes(30);

        // Act
        order.Complete(later);

        // Assert
        order.Status.ShouldBe(OrderStatus.Completed);
        order.CompletedAt.ShouldBe(later);
    }

    [Test]
    public void Complete_AlreadyCompleted_ThrowsNotInProgress()
    {
        // Arrange
        var order = Order.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), [Line(Guid.CreateVersion7(), 450m, 1)], Now);
        order.Complete(Now);

        // Act
        var act = () => order.Complete(Now);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Order.NotInProgress");
    }

    private static OrderLine Line(Guid dishId, decimal unitPrice, int quantity) =>
        new(dishId, "Plato", Money.Create(unitPrice), quantity);
}
