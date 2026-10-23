namespace Restaurant.Domain.Dishes;

public interface IDishRepository
{
    Task<Dish?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dish>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dish>> ListAsync(DishCategory? category, bool? isActive, CancellationToken cancellationToken);

    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> AnyUsingIngredientAsync(Guid ingredientId, CancellationToken cancellationToken);

    void Add(Dish dish);
}
