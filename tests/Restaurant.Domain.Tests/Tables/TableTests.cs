using Restaurant.Domain.Common;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Domain.Tests.Tables;

[TestFixture]
public sealed class TableTests
{
    [Test]
    public void Create_ValidData_StartsAvailable()
    {
        // Arrange
        // Act
        var table = Table.Create(5, 4, "Terraza");

        // Assert
        table.Number.ShouldBe(5);
        table.Capacity.ShouldBe(4);
        table.Description.ShouldBe("Terraza");
        table.Status.ShouldBe(TableStatus.Available);
        table.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Create_EmptyDescription_StoresNull()
    {
        // Arrange
        // Act
        var table = Table.Create(5, 4, "   ");

        // Assert
        table.Description.ShouldBeNull();
    }

    [TestCase(0)]
    [TestCase(1000)]
    public void Create_NumberOutOfRange_ThrowsInvalidNumber(int number)
    {
        // Arrange
        // Act
        var act = () => Table.Create(number, 4, null);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.InvalidNumber");
    }

    [TestCase(0)]
    [TestCase(21)]
    public void Create_CapacityOutOfRange_ThrowsInvalidCapacity(int capacity)
    {
        // Arrange
        // Act
        var act = () => Table.Create(5, capacity, null);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.InvalidCapacity");
    }

    [Test]
    public void Create_DescriptionOverOneHundredCharacters_ThrowsInvalidDescription()
    {
        // Arrange
        var description = new string('d', 101);

        // Act
        var act = () => Table.Create(5, 4, description);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.InvalidDescription");
    }

    [Test]
    public void Create_BoundaryValues_AreAccepted()
    {
        // Arrange
        // Act
        var table = Table.Create(999, 20, new string('d', 100));

        // Assert
        table.Number.ShouldBe(999);
        table.Capacity.ShouldBe(20);
    }

    [Test]
    public void UpdateDetails_AvailableTable_ChangesCapacityAndDescription()
    {
        // Arrange
        var table = Table.Create(5, 4, "Terraza");

        // Act
        table.UpdateDetails(6, "");

        // Assert
        table.Capacity.ShouldBe(6);
        table.Description.ShouldBeNull();
    }

    [Test]
    public void UpdateDetails_InServiceTable_ThrowsNotAvailable()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();

        // Act
        var act = () => table.UpdateDetails(6, null);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.NotAvailable");
    }

    [Test]
    public void StartService_AvailableTable_BecomesInService()
    {
        // Arrange
        var table = Table.Create(5, 4, null);

        // Act
        table.StartService();

        // Assert
        table.Status.ShouldBe(TableStatus.InService);
    }

    [Test]
    public void StartService_InServiceTable_ThrowsInvalidTransition()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();

        // Act
        var act = () => table.StartService();

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.InvalidTransition");
    }

    [Test]
    public void MarkServed_AvailableTable_ThrowsInvalidTransition()
    {
        // Arrange
        var table = Table.Create(5, 4, null);

        // Act
        var act = () => table.MarkServed();

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.InvalidTransition");
    }

    [Test]
    public void MarkServed_InServiceTable_BecomesServed()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();

        // Act
        table.MarkServed();

        // Assert
        table.Status.ShouldBe(TableStatus.Served);
    }

    [Test]
    public void Release_InServiceTable_ThrowsInvalidTransition()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();

        // Act
        var act = () => table.Release();

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Table.InvalidTransition");
    }

    [Test]
    public void Release_ServedTable_BecomesAvailable()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();
        table.MarkServed();

        // Act
        table.Release();

        // Assert
        table.Status.ShouldBe(TableStatus.Available);
    }
}
