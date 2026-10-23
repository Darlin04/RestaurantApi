using Moq;

using Restaurant.Application.Identity;
using Restaurant.Application.Identity.CreateUser;
using Restaurant.Domain.Common;

using Shouldly;

namespace Restaurant.Application.Tests.Identity;

[TestFixture]
public sealed class CreateUserHandlerTests
{
    [Test]
    public async Task HandleAsync_ValidCommand_ReturnsResponse()
    {
        // Arrange
        var command = new CreateUserCommand("nuevo@test.local", "Juan", "Perez", "Test-Passw0rd!", "Waiter");
        var expected = new UserResponse(Guid.CreateVersion7(), command.Email, command.FirstName, command.LastName, command.Role, true);
        var mock = new Mock<IUserManagementService>();
        mock.Setup(x => x.CreateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new CreateUserHandler(mock.Object);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expected);
    }

    [Test]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflict()
    {
        // Arrange
        var command = new CreateUserCommand("dup@test.local", "Juan", "Perez", "Test-Passw0rd!", "Waiter");
        var mock = new Mock<IUserManagementService>();
        mock.Setup(x => x.CreateAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("User.DuplicateEmail", $"Ya existe un usuario con el correo '{command.Email}'."));
        var handler = new CreateUserHandler(mock.Object);

        // Act
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("User.DuplicateEmail");
    }
}
