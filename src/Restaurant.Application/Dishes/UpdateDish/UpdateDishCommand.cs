using Restaurant.Domain.Dishes;

namespace Restaurant.Application.Dishes.UpdateDish;

public sealed record UpdateDishCommand(
    Guid Id,
    string Name,
    decimal Price,
    int Servings,
    DishCategory Category,
    IReadOnlyList<Guid> IngredientIds);
