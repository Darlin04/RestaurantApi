using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients;
using Restaurant.Application.Ingredients.RenameIngredient;
using Restaurant.Domain.Common;
using Restaurant.Domain.Ingredients;

using Shouldly;

namespace Restaurant.Application.Tests.Ingredients;

[TestFixture]
public sealed class RenameIngredientHandlerTests
{
    [Test]
    public async Task HandleAsync_UniqueName_RenamesAndSaves()
    {
        // Arrange
        var ingredient = Ingredient.Create("Tomate");
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        ingredients.Setup(x => x.ExistsByNormalizedNameAsync("TOMATE CHERRY", ingredient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new RenameIngredientHandler(ingredients.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(new RenameIngredientCommand(ingredient.Id, "Tomate cherry"), CancellationToken.None);

        // Assert
        result.ShouldBe(IngredientResponse.From(ingredient));
        result.Name.ShouldBe("Tomate cherry");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_NameOfAnotherIngredient_ThrowsConflictAndDoesNotSave()
    {
        // Arrange
        var ingredient = Ingredient.Create("Tomate");
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        ingredients.Setup(x => x.ExistsByNormalizedNameAsync("QUESO", ingredient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new RenameIngredientHandler(ingredients.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(new RenameIngredientCommand(ingredient.Id, "Queso"), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("Ingredient.DuplicateName");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_UnknownIngredient_ThrowsNotFound()
    {
        // Arrange
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ingredient?)null);
        var handler = new RenameIngredientHandler(ingredients.Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = () => handler.HandleAsync(new RenameIngredientCommand(Guid.NewGuid(), "Queso"), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Ingredient.NotFound");
    }
}
