using Restaurant.Application.Abstractions;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Ingredients.CreateIngredient;

public sealed class CreateIngredientHandler(IIngredientRepository ingredients, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateIngredientCommand, IngredientResponse>
{
    public async Task<IngredientResponse> HandleAsync(CreateIngredientCommand command, CancellationToken cancellationToken)
    {
        var ingredient = Ingredient.Create(command.Name);
        if (await ingredients.ExistsByNormalizedNameAsync(ingredient.NormalizedName, null, cancellationToken))
        {
            throw IngredientErrors.DuplicateName(ingredient.Name.Value);
        }

        ingredients.Add(ingredient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return IngredientResponse.From(ingredient);
    }
}
