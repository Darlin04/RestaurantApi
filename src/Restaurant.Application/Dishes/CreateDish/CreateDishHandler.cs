using Restaurant.Application.Abstractions;
using Restaurant.Domain.Common;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Ingredients;

namespace Restaurant.Application.Dishes.CreateDish;

public sealed class CreateDishHandler(
    IDishRepository dishes,
    IIngredientRepository ingredients,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateDishCommand, DishResponse>
{
    public async Task<DishResponse> HandleAsync(CreateDishCommand command, CancellationToken cancellationToken)
    {
        var price = Money.Create(command.Price);
        var ingredientIds = command.IngredientIds.Distinct().ToList();
        var found = await ingredients.GetByIdsAsync(ingredientIds, cancellationToken);

        var missing = ingredientIds.Where(id => found.All(i => i.Id != id)).ToList();
        if (missing.Count > 0)
        {
            throw IngredientErrors.NotFound(missing[0]);
        }

        var dish = Dish.Create(command.Name, price, command.Servings, command.Category, ingredientIds);
        if (await dishes.ExistsByNormalizedNameAsync(dish.NormalizedName, null, cancellationToken))
        {
            throw DishErrors.DuplicateName(dish.Name.Value);
        }

        dishes.Add(dish);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DishResponse.From(dish, found);
    }
}
