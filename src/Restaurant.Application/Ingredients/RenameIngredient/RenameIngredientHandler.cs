using Restaurant.Application.Abstractions;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Ingredients.RenameIngredient;

public sealed class RenameIngredientHandler(IIngredientRepository ingredients, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameIngredientCommand, IngredientResponse>
{
    public async Task<IngredientResponse> HandleAsync(RenameIngredientCommand command, CancellationToken cancellationToken)
    {
        var ingredient = await ingredients.GetByIdAsync(command.Id, cancellationToken)
            ?? throw IngredientErrors.NotFound(command.Id);

        ingredient.Rename(command.Name);
        if (await ingredients.ExistsByNormalizedNameAsync(ingredient.NormalizedName, ingredient.Id, cancellationToken))
        {
            throw IngredientErrors.DuplicateName(ingredient.Name.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return IngredientResponse.From(ingredient);
    }
}
