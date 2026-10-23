using Restaurant.Application.Abstractions;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Dishes.ListDishes;

public sealed class ListDishesHandler(IDishRepository dishes, IIngredientRepository ingredients)
    : IQueryHandler<ListDishesQuery, IReadOnlyList<DishResponse>>
{
    public async Task<IReadOnlyList<DishResponse>> HandleAsync(ListDishesQuery query, CancellationToken cancellationToken)
    {
        var items = await dishes.ListAsync(query.Category, query.IsActive, cancellationToken);
        var ingredientIds = items.SelectMany(d => d.Ingredients.Select(i => i.IngredientId)).Distinct().ToList();
        var found = await ingredients.GetByIdsAsync(ingredientIds, cancellationToken);

        return items.Select(dish => DishResponse.From(dish, ForDish(dish, found))).ToList();
    }

    private static IReadOnlyList<Ingredient> ForDish(Dish dish, IReadOnlyList<Ingredient> found)
    {
        var ids = dish.Ingredients.Select(i => i.IngredientId).ToHashSet();
        return found.Where(i => ids.Contains(i.Id)).ToList();
    }
}
