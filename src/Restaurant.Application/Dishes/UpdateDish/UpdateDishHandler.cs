using Restaurant.Application.Abstractions;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Dishes.UpdateDish;

public sealed class UpdateDishHandler(
    IDishRepository dishes,
    IIngredientRepository ingredients,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateDishCommand, DishResponse>
{
    public async Task<DishResponse> HandleAsync(UpdateDishCommand command, CancellationToken cancellationToken)
    {
        var dish = await dishes.GetByIdAsync(command.Id, cancellationToken)
            ?? throw DishErrors.NotFound(command.Id);

        var price = Money.Create(command.Price);
        var ingredientIds = command.IngredientIds.Distinct().ToList();
        var found = await ingredients.GetByIdsAsync(ingredientIds, cancellationToken);

        var missing = ingredientIds.Where(id => found.All(i => i.Id != id)).ToList();
        if (missing.Count > 0)
        {
            throw IngredientErrors.NotFound(missing[0]);
        }

        dish.Update(command.Name, price, command.Servings, command.Category, ingredientIds);
        if (await dishes.ExistsByNormalizedNameAsync(dish.NormalizedName, dish.Id, cancellationToken))
        {
            throw DishErrors.DuplicateName(dish.Name.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DishResponse.From(dish, found);
    }
}
