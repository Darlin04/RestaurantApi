using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Ingredients;

public sealed record IngredientResponse(Guid Id, string Name)
{
    public static IngredientResponse From(Ingredient ingredient) => new(ingredient.Id, ingredient.Name.Value);
}
