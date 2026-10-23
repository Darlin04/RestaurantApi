using Restaurant.Domain.Dishes;

namespace Restaurant.Application.Dishes.CreateDish;

public sealed record CreateDishCommand(
    string Name,
    decimal Price,
    int Servings,
    DishCategory Category,
    IReadOnlyList<Guid> IngredientIds);
