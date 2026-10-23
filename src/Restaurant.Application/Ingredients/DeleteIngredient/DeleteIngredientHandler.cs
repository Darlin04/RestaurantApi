using Restaurant.Application.Abstractions;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Ingredients.DeleteIngredient;

public sealed class DeleteIngredientHandler(
    IIngredientRepository ingredients,
    IDishRepository dishes,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteIngredientCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteIngredientCommand command, CancellationToken cancellationToken)
    {
        var ingredient = await ingredients.GetByIdAsync(command.Id, cancellationToken)
            ?? throw IngredientErrors.NotFound(command.Id);

        if (await dishes.AnyUsingIngredientAsync(command.Id, cancellationToken))
        {
            throw IngredientErrors.InUse();
        }

        ingredients.Remove(ingredient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
