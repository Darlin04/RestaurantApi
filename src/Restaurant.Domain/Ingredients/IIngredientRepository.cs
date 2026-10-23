namespace Restaurant.Domain.Ingredients;

public interface IIngredientRepository
{
    Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Ingredient>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludeId, CancellationToken cancellationToken);

    void Add(Ingredient ingredient);

    void Remove(Ingredient ingredient);
}
