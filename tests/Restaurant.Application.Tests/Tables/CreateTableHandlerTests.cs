using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.CreateTable;
using Restaurant.Domain.Common;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Tables;

[TestFixture]
public sealed class CreateTableHandlerTests
{
    [Test]
    public async Task HandleAsync_NewNumber_AddsTableAndSaves()
    {
        // Arrange
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.ExistsByNumberAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTableHandler(tables.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(new CreateTableCommand(5, 4, "Terraza"), CancellationToken.None);

        // Assert
        result.Number.ShouldBe(5);
        result.Status.ShouldBe("Available");
        tables.Verify(x => x.Add(It.Is<Table>(t => t.Number == 5)), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_DuplicateNumber_ThrowsConflictAndDoesNotSave()
    {
        // Arrange
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.ExistsByNumberAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTableHandler(tables.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(new CreateTableCommand(5, 4, null), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("Table.DuplicateNumber");
        tables.Verify(x => x.Add(It.IsAny<Table>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
