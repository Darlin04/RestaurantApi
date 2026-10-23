namespace Restaurant.Domain.Dishes;

public sealed class DishIngredient
{
    private DishIngredient()
    {
    }

    internal DishIngredient(Guid ingredientId)
    {
        IngredientId = ingredientId;
    }

    public Guid IngredientId { get; private set; }
}
