using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients.DeleteIngredient;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

using Shouldly;

namespace Restaurant.Application.Tests.Ingredients;

[TestFixture]
public sealed class DeleteIngredientHandlerTests
{
    [Test]
    public async Task HandleAsync_UnusedIngredient_RemovesAndSaves()
    {
        // Arrange
        var ingredient = Ingredient.Create("Ajo");
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.AnyUsingIngredientAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new DeleteIngredientHandler(ingredients.Object, dishes.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(new DeleteIngredientCommand(ingredient.Id), CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        ingredients.Verify(x => x.Remove(ingredient), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_IngredientUsedByDish_ThrowsInUseAndDoesNotRemove()
    {
        // Arrange
        var ingredient = Ingredient.Create("Ajo");
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.AnyUsingIngredientAsync(ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new DeleteIngredientHandler(ingredients.Object, dishes.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(new DeleteIngredientCommand(ingredient.Id), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("Ingredient.InUse");
        ingredients.Verify(x => x.Remove(It.IsAny<Ingredient>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_UnknownIngredient_ThrowsNotFound()
    {
        // Arrange
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ingredient?)null);
        var handler = new DeleteIngredientHandler(ingredients.Object, new Mock<IDishRepository>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = () => handler.HandleAsync(new DeleteIngredientCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Ingredient.NotFound");
    }
}
