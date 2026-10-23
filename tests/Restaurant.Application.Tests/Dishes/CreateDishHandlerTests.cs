using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes;
using Restaurant.Application.Dishes.CreateDish;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

using Shouldly;

namespace Restaurant.Application.Tests.Dishes;

[TestFixture]
public sealed class CreateDishHandlerTests
{
    [Test]
    public async Task HandleAsync_ExistingIngredients_AddsDishWithIngredientsOrderedByName()
    {
        // Arrange
        var pollo = Ingredient.Create("Pollo");
        var arroz = Ingredient.Create("Arroz");
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.ExistsByNormalizedNameAsync("LOCRIO", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { pollo, arroz });
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateDishHandler(dishes.Object, ingredients.Object, unitOfWork.Object);
        var command = new CreateDishCommand("Locrio", 450.00m, 1, DishCategory.MainCourse, [pollo.Id, arroz.Id]);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Name.ShouldBe("Locrio");
        result.IsActive.ShouldBeTrue();
        result.Ingredients.Select(i => i.Name).ShouldBe(["Arroz", "Pollo"]);
        dishes.Verify(x => x.Add(It.Is<Dish>(d => d.Name.Value == "Locrio")), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_MissingIngredient_ThrowsIngredientNotFoundWithId()
    {
        // Arrange
        var missingId = Guid.CreateVersion7();
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient>());
        var dishes = new Mock<IDishRepository>();
        var handler = new CreateDishHandler(dishes.Object, ingredients.Object, new Mock<IUnitOfWork>().Object);
        var command = new CreateDishCommand("Locrio", 450m, 1, DishCategory.MainCourse, [missingId]);

        // Act
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Ingredient.NotFound");
        ex.Message.ShouldContain(missingId.ToString());
        dishes.Verify(x => x.Add(It.IsAny<Dish>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_DuplicateName_ThrowsConflictAndDoesNotSave()
    {
        // Arrange
        var pollo = Ingredient.Create("Pollo");
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.ExistsByNormalizedNameAsync("LOCRIO", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { pollo });
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateDishHandler(dishes.Object, ingredients.Object, unitOfWork.Object);
        var command = new CreateDishCommand("Locrio", 450m, 1, DishCategory.MainCourse, [pollo.Id]);

        // Act
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("Dish.DuplicateName");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_PriceWithThreeDecimals_ThrowsTooManyDecimals()
    {
        // Arrange
        var ingredients = new Mock<IIngredientRepository>();
        var handler = new CreateDishHandler(new Mock<IDishRepository>().Object, ingredients.Object, new Mock<IUnitOfWork>().Object);
        var command = new CreateDishCommand("Locrio", 450.001m, 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Act
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Money.TooManyDecimals");
    }
}
