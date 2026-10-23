using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;

using Shouldly;

namespace Restaurant.Domain.Tests.Dishes;

[TestFixture]
public sealed class DishTests
{
    [Test]
    public void Create_ValidData_IsActiveAndKeepsIngredients()
    {
        // Arrange
        var ingredientIds = new[] { Guid.CreateVersion7(), Guid.CreateVersion7() };

        // Act
        var dish = Dish.Create("Locrio", Money.Create(450.00m), 1, DishCategory.MainCourse, ingredientIds);

        // Assert
        dish.IsActive.ShouldBeTrue();
        dish.NormalizedName.ShouldBe("LOCRIO");
        dish.Ingredients.Select(i => i.IngredientId).ShouldBe(ingredientIds);
    }

    [Test]
    public void Create_DuplicateIngredientIds_RemovesDuplicates()
    {
        // Arrange
        var id = Guid.CreateVersion7();

        // Act
        var dish = Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, [id, id]);

        // Assert
        dish.Ingredients.Count.ShouldBe(1);
    }

    [Test]
    public void Create_NoIngredients_ThrowsIngredientsRequired()
    {
        // Arrange
        // Act
        var act = () => Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, []);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.IngredientsRequired");
    }

    [Test]
    public void Create_ThirtyOneIngredients_ThrowsTooManyIngredients()
    {
        // Arrange
        var ids = Enumerable.Range(0, 31).Select(_ => Guid.CreateVersion7()).ToList();

        // Act
        var act = () => Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, ids);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.TooManyIngredients");
    }

    [TestCase(0)]
    [TestCase(21)]
    public void Create_ServingsOutOfRange_ThrowsInvalidServings(int servings)
    {
        // Arrange
        // Act
        var act = () => Dish.Create("Locrio", Money.Create(10m), servings, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.InvalidServings");
    }

    [Test]
    public void Create_ZeroPrice_ThrowsInvalidPrice()
    {
        // Arrange
        // Act
        var act = () => Dish.Create("Locrio", Money.Zero, 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.InvalidPrice");
    }

    [Test]
    public void Create_PriceAboveMaximum_ThrowsInvalidPrice()
    {
        // Arrange
        // Act
        var act = () => Dish.Create("Locrio", Money.Create(1_000_000m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.InvalidPrice");
    }

    [Test]
    public void Create_OneCharacterName_ThrowsInvalidName()
    {
        // Arrange
        // Act
        var act = () => Dish.Create("A", Money.Create(10m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.InvalidName");
    }

    [Test]
    public void Update_InactiveDish_ThrowsInactive()
    {
        // Arrange
        var dish = Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        dish.Deactivate();

        // Act
        var act = () => dish.Update("Locrio 2", Money.Create(12m), 2, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.Inactive");
    }

    [Test]
    public void Update_ActiveDish_ReplacesIngredients()
    {
        // Arrange
        var dish = Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        var newIngredient = Guid.CreateVersion7();

        // Act
        dish.Update("Locrio 2", Money.Create(12m), 2, DishCategory.Dessert, [newIngredient]);

        // Assert
        dish.Name.Value.ShouldBe("Locrio 2");
        dish.Servings.ShouldBe(2);
        dish.Category.ShouldBe(DishCategory.Dessert);
        dish.Ingredients.Select(i => i.IngredientId).ShouldBe([newIngredient]);
    }

    [Test]
    public void Deactivate_Twice_StaysInactive()
    {
        // Arrange
        var dish = Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Act
        dish.Deactivate();
        dish.Deactivate();

        // Assert
        dish.IsActive.ShouldBeFalse();
    }

    [Test]
    public void ToOrderLine_InactiveDish_ThrowsInactive()
    {
        // Arrange
        var dish = Dish.Create("Locrio", Money.Create(10m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);
        dish.Deactivate();

        // Act
        var act = () => dish.ToOrderLine(1);

        // Assert
        Should.Throw<DomainException>(act).Code.ShouldBe("Dish.Inactive");
    }

    [Test]
    public void ToOrderLine_ActiveDish_CopiesNameAndPrice()
    {
        // Arrange
        var dish = Dish.Create("Locrio", Money.Create(450m), 1, DishCategory.MainCourse, [Guid.CreateVersion7()]);

        // Act
        var line = dish.ToOrderLine(2);

        // Assert
        line.DishId.ShouldBe(dish.Id);
        line.DishName.ShouldBe("Locrio");
        line.UnitPrice.ShouldBe(Money.Create(450m));
        line.Quantity.ShouldBe(2);
    }
}
