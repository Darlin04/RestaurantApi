using Restaurant.Application.Abstractions;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Ingredients.GetIngredient;

public sealed class GetIngredientHandler(IIngredientRepository ingredients)
    : IQueryHandler<GetIngredientQuery, IngredientResponse>
{
    public async Task<IngredientResponse> HandleAsync(GetIngredientQuery query, CancellationToken cancellationToken)
    {
        var ingredient = await ingredients.GetByIdAsync(query.Id, cancellationToken)
            ?? throw IngredientErrors.NotFound(query.Id);

        return IngredientResponse.From(ingredient);
    }
}
