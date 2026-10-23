using Restaurant.Domain.Common;
using Restaurant.Domain.Orders;

namespace Restaurant.Domain.Dishes;

public sealed class Dish : AggregateRoot
{
    private const decimal MaxPrice = 999_999.99m;
    private const int MinServings = 1;
    private const int MaxServings = 20;
    private const int MaxIngredients = 30;

    private readonly List<DishIngredient> _ingredients = [];

    private Dish()
    {
    }

    public DishName Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = string.Empty;

    public Money Price { get; private set; } = null!;

    public int Servings { get; private set; }

    public DishCategory Category { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<DishIngredient> Ingredients => _ingredients;

    public static Dish Create(string name, Money price, int servings, DishCategory category, IEnumerable<Guid> ingredientIds)
    {
        var dish = new Dish { Id = Guid.CreateVersion7(), IsActive = true };
        dish.ApplyDetails(name, price, servings, category, ingredientIds);
        return dish;
    }

    public void Update(string name, Money price, int servings, DishCategory category, IEnumerable<Guid> ingredientIds)
    {
        EnsureActive();
        ApplyDetails(name, price, servings, category, ingredientIds);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public OrderLine ToOrderLine(int quantity)
    {
        EnsureActive();
        return new OrderLine(Id, Name.Value, Price, quantity);
    }

    private void ApplyDetails(string name, Money price, int servings, DishCategory category, IEnumerable<Guid> ingredientIds)
    {
        var dishName = DishName.Create(name);
        ValidatePrice(price);
        ValidateServings(servings);

        var ids = ingredientIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            throw DishErrors.IngredientsRequired();
        }

        if (ids.Count > MaxIngredients)
        {
            throw DishErrors.TooManyIngredients();
        }

        Name = dishName;
        NormalizedName = dishName.Value.ToUpperInvariant();
        Price = price;
        Servings = servings;
        Category = category;
        _ingredients.Clear();
        _ingredients.AddRange(ids.Select(id => new DishIngredient(id)));
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw DishErrors.Inactive(Name.Value);
        }
    }

    private static void ValidatePrice(Money price)
    {
        if (price.Amount <= 0 || price.Amount > MaxPrice)
        {
            throw DishErrors.InvalidPrice();
        }
    }

    private static void ValidateServings(int servings)
    {
        if (servings is < MinServings or > MaxServings)
        {
            throw DishErrors.InvalidServings();
        }
    }
}
