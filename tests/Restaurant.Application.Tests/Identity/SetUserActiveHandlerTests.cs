using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Application.Identity.SetUserActive;
using Restaurant.Domain.Common;

using Shouldly;

namespace Restaurant.Application.Tests.Identity;

[TestFixture]
public sealed class SetUserActiveHandlerTests
{
    [Test]
    public async Task HandleAsync_DeactivateOtherUser_CallsService()
    {
        // Arrange
        var targetId = Guid.CreateVersion7();
        var users = new Mock<IUserActivationService>();
        var currentUser = CreateCurrentUser(Guid.CreateVersion7());
        var handler = new SetUserActiveHandler(users.Object, currentUser.Object);

        // Act
        var result = await handler.HandleAsync(new SetUserActiveCommand(targetId, false), CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        users.Verify(x => x.SetActiveAsync(targetId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_DeactivateSelf_ThrowsCannotDeactivateSelf()
    {
        // Arrange
        var adminId = Guid.CreateVersion7();
        var users = new Mock<IUserActivationService>();
        var currentUser = CreateCurrentUser(adminId);
        var handler = new SetUserActiveHandler(users.Object, currentUser.Object);

        // Act
        var act = () => handler.HandleAsync(new SetUserActiveCommand(adminId, false), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("User.CannotDeactivateSelf");
        users.Verify(x => x.SetActiveAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_ActivateSelf_CallsService()
    {
        // Arrange
        var adminId = Guid.CreateVersion7();
        var users = new Mock<IUserActivationService>();
        var currentUser = CreateCurrentUser(adminId);
        var handler = new SetUserActiveHandler(users.Object, currentUser.Object);

        // Act
        await handler.HandleAsync(new SetUserActiveCommand(adminId, true), CancellationToken.None);

        // Assert
        users.Verify(x => x.SetActiveAsync(adminId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<ICurrentUser> CreateCurrentUser(Guid userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(userId);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        return currentUser;
    }
}
