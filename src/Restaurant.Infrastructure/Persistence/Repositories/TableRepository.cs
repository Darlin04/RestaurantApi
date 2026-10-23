using Microsoft.EntityFrameworkCore;

using Restaurant.Domain.Tables;

namespace Restaurant.Infrastructure.Persistence.Repositories;

public sealed class TableRepository(RestaurantDbContext dbContext) : ITableRepository
{
    public Task<Table?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Table>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Table>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var idArray = ids.ToArray();
        return await dbContext.Set<Table>()
            .AsNoTracking()
            .Where(t => idArray.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Table>> ListAsync(TableStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Table>().AsNoTracking();
        if (status is { } value)
        {
            query = query.Where(t => t.Status == value);
        }

        return await query.OrderBy(t => t.Number).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNumberAsync(int number, CancellationToken cancellationToken) =>
        dbContext.Set<Table>().AnyAsync(t => t.Number == number, cancellationToken);

    public void Add(Table table) => dbContext.Set<Table>().Add(table);
}
