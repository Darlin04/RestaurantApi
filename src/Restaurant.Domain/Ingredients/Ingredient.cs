using Restaurant.Domain.Common;

namespace Restaurant.Domain.Ingredients;

public sealed class Ingredient : AggregateRoot
{
    private Ingredient()
    {
    }

    public IngredientName Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = string.Empty;

    public static Ingredient Create(string name)
    {
        var ingredient = new Ingredient { Id = Guid.CreateVersion7() };
        ingredient.ApplyName(name);
        return ingredient;
    }

    public void Rename(string name) => ApplyName(name);

    private void ApplyName(string name)
    {
        Name = IngredientName.Create(name);
        NormalizedName = Name.Value.ToUpperInvariant();
    }
}
