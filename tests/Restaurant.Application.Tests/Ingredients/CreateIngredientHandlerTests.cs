using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients;
using Restaurant.Application.Ingredients.CreateIngredient;
using Restaurant.Domain.Common;
using Restaurant.Domain.Ingredients;

using Shouldly;

namespace Restaurant.Application.Tests.Ingredients;

[TestFixture]
public sealed class CreateIngredientHandlerTests
{
    [Test]
    public async Task HandleAsync_NewName_AddsIngredientAndSaves()
    {
        // Arrange
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.ExistsByNormalizedNameAsync("TOMATE", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateIngredientHandler(ingredients.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(new CreateIngredientCommand("  Tomate  "), CancellationToken.None);

        // Assert
        result.Name.ShouldBe("Tomate");
        result.Id.ShouldNotBe(Guid.Empty);
        ingredients.Verify(x => x.Add(It.Is<Ingredient>(i => i.Name.Value == "Tomate")), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_DuplicateNameIgnoringCase_ThrowsConflictAndDoesNotSave()
    {
        // Arrange
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.ExistsByNormalizedNameAsync("TOMATE", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateIngredientHandler(ingredients.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(new CreateIngredientCommand("TOMATE"), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("Ingredient.DuplicateName");
        ingredients.Verify(x => x.Add(It.IsAny<Ingredient>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
