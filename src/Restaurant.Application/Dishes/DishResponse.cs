using Restaurant.Application.Ingredients;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Dishes;

public sealed record DishResponse(
    Guid Id,
    string Name,
    decimal Price,
    int Servings,
    string Category,
    bool IsActive,
    IReadOnlyList<IngredientResponse> Ingredients)
{
    public static DishResponse From(Dish dish, IReadOnlyList<Ingredient> ingredients) =>
        new(
            dish.Id,
            dish.Name.Value,
            dish.Price.Amount,
            dish.Servings,
            dish.Category.ToString(),
            dish.IsActive,
            ingredients.OrderBy(i => i.Name.Value).Select(IngredientResponse.From).ToList());
}
