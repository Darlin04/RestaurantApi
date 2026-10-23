using Moq;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes;
using Restaurant.Application.Dishes.UpdateDish;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

using Shouldly;

namespace Restaurant.Application.Tests.Dishes;

[TestFixture]
public sealed class UpdateDishHandlerTests
{
    [Test]
    public async Task HandleAsync_ActiveDish_UpdatesAndSaves()
    {
        // Arrange
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.GetByIdAsync(dish.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        dishes.Setup(x => x.ExistsByNormalizedNameAsync("LOCRIO 2", dish.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { pollo });
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateDishHandler(dishes.Object, ingredients.Object, unitOfWork.Object);

        // Act
        var result = await handler.HandleAsync(
            new UpdateDishCommand(dish.Id, "Locrio 2", 500m, 2, DishCategory.MainCourse, [pollo.Id]),
            CancellationToken.None);

        // Assert
        result.Name.ShouldBe("Locrio 2");
        result.Price.ShouldBe(500m);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_UnknownDish_ThrowsNotFound()
    {
        // Arrange
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Dish?)null);
        var handler = new UpdateDishHandler(dishes.Object, new Mock<IIngredientRepository>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = () => handler.HandleAsync(
            new UpdateDishCommand(Guid.NewGuid(), "Locrio", 450m, 1, DishCategory.MainCourse, [Guid.NewGuid()]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<NotFoundException>(act());
        ex.Code.ShouldBe("Dish.NotFound");
    }

    [Test]
    public async Task HandleAsync_InactiveDish_ThrowsInactiveAndDoesNotSave()
    {
        // Arrange
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        dish.Deactivate();
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.GetByIdAsync(dish.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { pollo });
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateDishHandler(dishes.Object, ingredients.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(
            new UpdateDishCommand(dish.Id, "Locrio 2", 500m, 2, DishCategory.MainCourse, [pollo.Id]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<DomainException>(act());
        ex.Code.ShouldBe("Dish.Inactive");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_NameOfAnotherDish_ThrowsConflictAndDoesNotSave()
    {
        // Arrange
        var pollo = Ingredient.Create("Pollo");
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [pollo.Id]);
        var dishes = new Mock<IDishRepository>();
        dishes.Setup(x => x.GetByIdAsync(dish.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dish);
        dishes.Setup(x => x.ExistsByNormalizedNameAsync("FLAN", dish.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { pollo });
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdateDishHandler(dishes.Object, ingredients.Object, unitOfWork.Object);

        // Act
        var act = () => handler.HandleAsync(
            new UpdateDishCommand(dish.Id, "Flan", 450m, 1, DishCategory.MainCourse, [pollo.Id]),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ConflictException>(act());
        ex.Code.ShouldBe("Dish.DuplicateName");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
