using Restaurant.Application.Abstractions;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Ingredients.ListIngredients;

public sealed class ListIngredientsHandler(IIngredientRepository ingredients)
    : IQueryHandler<ListIngredientsQuery, IReadOnlyList<IngredientResponse>>
{
    public async Task<IReadOnlyList<IngredientResponse>> HandleAsync(ListIngredientsQuery query, CancellationToken cancellationToken)
    {
        var items = await ingredients.ListAsync(cancellationToken);
        return items.Select(IngredientResponse.From).ToList();
    }
}
