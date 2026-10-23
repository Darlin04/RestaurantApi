using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.UpdateTable;
using Restaurant.Domain.Common;
using Restaurant.Domain.Tables;

using Shouldly;

namespace Restaurant.Application.Tests.Tables;

[TestFixture]
public sealed class UpdateTableHandlerTests
{
    [Test]
    public async Task HandleAsync_AvailableTable_UpdatesAndSaves()
    {
        // Arrange
        var table = Table.Create(5, 4, "Terraza");
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.GetByIdAsync(table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(table);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateTableHandler(tables.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(new UpdateTableCommand(table.Id, 6, ""), CancellationToken.None);

        // Assert
        result.Capacity.ShouldBe(6);
        result.Description.ShouldBeNull();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_UnknownTable_ThrowsNotFound()
    {
        // Arrange
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Table?)null);
        var handler = new UpdateTableHandler(tables.Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = () => handler.HandleAsync(new UpdateTableCommand(Guid.NewGuid(), 4, null), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Table.NotFound");
    }

    [Test]
    public async Task HandleAsync_InServiceTable_ThrowsNotAvailableAndDoesNotSave()
    {
        // Arrange
        var table = Table.Create(5, 4, null);
        table.StartService();
        var tables = new Mock<ITableRepository>();
        tables.Setup(x => x.GetByIdAsync(table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(table);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateTableHandler(tables.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(new UpdateTableCommand(table.Id, 6, null), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Table.NotAvailable");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
