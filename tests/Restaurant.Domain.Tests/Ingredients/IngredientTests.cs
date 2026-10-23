using Restaurant.Domain.Common;
using Restaurant.Domain.Ingredients;

using Shouldly;

namespace Restaurant.Domain.Tests.Ingredients;

[TestFixture]
public sealed class IngredientTests
{
    [Test]
    public void Create_ValidName_TrimsAndSetsNormalizedName()
    {
        // Arrange
        const string name = "  Tomate  ";

        // Act
        var ingredient = Ingredient.Create(name);

        // Assert
        ingredient.Name.Value.ShouldBe("Tomate");
        ingredient.NormalizedName.ShouldBe("TOMATE");
        ingredient.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Create_OneCharacterName_ThrowsInvalidName()
    {
        // Arrange
        const string name = "A";

        // Act
        var act = () => Ingredient.Create(name);

        // Assert
        var ex = Should.Throw<DomainException>(act);
        ex.Code.ShouldBe("Ingredient.InvalidName");
    }

    [Test]
    public void Create_SixtyOneCharacterName_ThrowsInvalidName()
    {
        // Arrange
        var name = new string('a', 61);

        // Act
        var act = () => Ingredient.Create(name);

        // Assert
        var ex = Should.Throw<DomainException>(act);
        ex.Code.ShouldBe("Ingredient.InvalidName");
    }

    [Test]
    public void Create_SixtyCharacterName_IsAccepted()
    {
        // Arrange
        var name = new string('a', 60);

        // Act
        var ingredient = Ingredient.Create(name);

        // Assert
        ingredient.Name.Value.Length.ShouldBe(60);
    }

    [Test]
    public void Rename_ValidName_UpdatesNameAndNormalizedName()
    {
        // Arrange
        var ingredient = Ingredient.Create("Tomate");

        // Act
        ingredient.Rename("Queso crema");

        // Assert
        ingredient.Name.Value.ShouldBe("Queso crema");
        ingredient.NormalizedName.ShouldBe("QUESO CREMA");
    }

    [Test]
    public void Rename_InvalidName_ThrowsInvalidNameAndKeepsPreviousName()
    {
        // Arrange
        var ingredient = Ingredient.Create("Tomate");

        // Act
        var act = () => ingredient.Rename("A");

        // Assert
        var ex = Should.Throw<DomainException>(act);
        ex.Code.ShouldBe("Ingredient.InvalidName");
        ingredient.Name.Value.ShouldBe("Tomate");
    }
}
