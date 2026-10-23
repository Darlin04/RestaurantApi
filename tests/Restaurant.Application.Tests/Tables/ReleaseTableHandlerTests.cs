using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.ReleaseTable;
using Restaurant.Domain.Common;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Tables;

[TestFixture]
public sealed class ReleaseTableHandlerTests
{
    [Test]
    public async Task HandleAsync_ServedTable_BecomesAvailableAndSaves()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();
        table.MarkServed();
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.GetByIdAsync(table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(table);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new ReleaseTableHandler(tables.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(new ReleaseTableCommand(table.Id), CancellationToken.None);

        // Assert
        result.Status.ShouldBe("Available");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_InServiceTable_ThrowsInvalidTransitionAndDoesNotSave()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.GetByIdAsync(table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(table);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new ReleaseTableHandler(tables.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(new ReleaseTableCommand(table.Id), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Table.InvalidTransition");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_UnknownTable_ThrowsNotFound()
    {
        // Arrange
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Table?)null);
        var handler = new ReleaseTableHandler(tables.Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = () => handler.HandleAsync(new ReleaseTableCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Table.NotFound");
    }
}
