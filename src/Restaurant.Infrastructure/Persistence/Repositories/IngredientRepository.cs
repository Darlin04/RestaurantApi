using Microsoft.EntityFrameworkCore;

using Restaurant.Domain.Ingredients;

namespace Restaurant.Infrastructure.Persistence.Repositories;

public sealed class IngredientRepository(RestaurantDbContext dbContext) : IIngredientRepository
{
    public Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Ingredient>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ingredient>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var idArray = ids.ToArray();
        return await dbContext.Set<Ingredient>()
            .AsNoTracking()
            .Where(i => idArray.Contains(i.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<Ingredient>()
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Ingredient>().Where(i => i.NormalizedName == normalizedName);
        if (excludeId is { } id)
        {
            query = query.Where(i => i.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(Ingredient ingredient) => dbContext.Set<Ingredient>().Add(ingredient);

    public void Remove(Ingredient ingredient) => dbContext.Set<Ingredient>().Remove(ingredient);
}
