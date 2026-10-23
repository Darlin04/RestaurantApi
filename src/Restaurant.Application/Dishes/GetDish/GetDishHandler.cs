using Restaurant.Application.Abstractions;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Dishes.GetDish;

public sealed class GetDishHandler(IDishRepository dishes, IIngredientRepository ingredients)
    : IQueryHandler<GetDishQuery, DishResponse>
{
    public async Task<DishResponse> HandleAsync(GetDishQuery query, CancellationToken cancellationToken)
    {
        var dish = await dishes.GetByIdAsync(query.Id, cancellationToken)
            ?? throw DishErrors.NotFound(query.Id);

        var ingredientIds = dish.Ingredients.Select(i => i.IngredientId).ToList();
        var found = await ingredients.GetByIdsAsync(ingredientIds, cancellationToken);
        return DishResponse.From(dish, found);
    }
}
