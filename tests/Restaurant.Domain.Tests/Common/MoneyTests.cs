using Restaurant.Domain.Common;

using Shouldly;

namespace Restaurant.Domain.Tests.Common;

[TestFixture]
public sealed class MoneyTests
{
    [Test]
    public void Create_NegativeAmount_ThrowsNegative()
    {
        // Arrange
        var amount = -1m;

        // Act
        var act = () => Money.Create(amount);

        // Assert
        var ex = Assert.Throws<DomainException>(() => act());
        ex!.Code.ShouldBe("Money.Negative");
    }

    [Test]
    public void Create_TooManyDecimals_ThrowsTooManyDecimals()
    {
        // Arrange
        var amount = 10.555m;

        // Act
        var act = () => Money.Create(amount);

        // Assert
        var ex = Assert.Throws<DomainException>(() => act());
        ex!.Code.ShouldBe("Money.TooManyDecimals");
    }

    [Test]
    public void Create_ValidAmount_ReturnsMoney()
    {
        // Arrange
        var amount = 10.55m;

        // Act
        var money = Money.Create(amount);

        // Assert
        money.Amount.ShouldBe(amount);
    }

    [Test]
    public void Zero_ReturnsZeroAmount()
    {
        // Arrange
        // Act
        var money = Money.Zero;

        // Assert
        money.Amount.ShouldBe(0m);
    }

    [Test]
    public void Add_TwoMonies_ReturnsSum()
    {
        // Arrange
        var left = Money.Create(10.50m);
        var right = Money.Create(5.25m);

        // Act
        var result = left + right;

        // Assert
        result.Amount.ShouldBe(15.75m);
    }

    [Test]
    public void Multiply_ByQuantity_ReturnsProduct()
    {
        // Arrange
        var money = Money.Create(450.00m);

        // Act
        var result = money.Multiply(2);

        // Assert
        result.Amount.ShouldBe(900.00m);
    }
}
