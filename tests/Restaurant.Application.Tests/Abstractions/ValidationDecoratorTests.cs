using FluentValidation;

using Moq;

using Restaurant.Application.Abstractions;

using Shouldly;

namespace Restaurant.Application.Tests.Abstractions;

[TestFixture]
public sealed class ValidationDecoratorTests
{
    public sealed record TestCommand(string Name);

    public sealed class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    private sealed class FakeHandler : ICommandHandler<TestCommand, string>
    {
        public bool Called { get; private set; }

        public Task<string> HandleAsync(TestCommand command, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult("ok");
        }
    }

    [Test]
    public async Task HandleAsync_InvalidCommand_ThrowsAndDoesNotInvokeInner()
    {
        // Arrange
        var command = new TestCommand(string.Empty);
        var inner = new FakeHandler();
        var validators = new List<IValidator<TestCommand>> { new TestValidator() };
        var decorator = new ValidationDecorator<TestCommand, string>(inner, validators);

        // Act
        var act = () => decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<ValidationException>(act());
        inner.Called.ShouldBeFalse();
    }

    [Test]
    public async Task HandleAsync_ValidCommand_DelegatesToInner()
    {
        // Arrange
        var command = new TestCommand("Tomate");
        var innerMock = new Mock<ICommandHandler<TestCommand, string>>();
        innerMock.Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync("ok");
        var validators = new List<IValidator<TestCommand>> { new TestValidator() };
        var decorator = new ValidationDecorator<TestCommand, string>(innerMock.Object, validators);

        // Act
        var result = await decorator.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe("ok");
        innerMock.Verify(x => x.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}
