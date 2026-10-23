using Microsoft.EntityFrameworkCore;

using Restaurant.Domain.Dishes;

namespace Restaurant.Infrastructure.Persistence.Repositories;

public sealed class DishRepository(RestaurantDbContext dbContext) : IDishRepository
{
    public Task<Dish?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Dish>().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Dish>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var idArray = ids.ToArray();
        return await dbContext.Set<Dish>()
            .AsNoTracking()
            .Where(d => idArray.Contains(d.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Dish>> ListAsync(DishCategory? category, bool? isActive, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Dish>().AsNoTracking();
        if (category is { } categoryValue)
        {
            query = query.Where(d => d.Category == categoryValue);
        }

        if (isActive is { } activeValue)
        {
            query = query.Where(d => d.IsActive == activeValue);
        }

        return await query.OrderBy(d => d.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Dish>().Where(d => d.NormalizedName == normalizedName);
        if (excludeId is { } id)
        {
            query = query.Where(d => d.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> AnyUsingIngredientAsync(Guid ingredientId, CancellationToken cancellationToken) =>
        dbContext.Set<Dish>().AnyAsync(d => d.Ingredients.Any(i => i.IngredientId == ingredientId), cancellationToken);

    public void Add(Dish dish) => dbContext.Set<Dish>().Add(dish);
}
