using Restaurant.Domain.Dishes;

namespace Restaurant.Application.Dishes.ListDishes;

public sealed record ListDishesQuery(DishCategory? Category, bool? IsActive);
