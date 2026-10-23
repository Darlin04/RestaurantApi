using Microsoft.EntityFrameworkCore;

using Restaurant.Domain.Orders;

namespace Restaurant.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(RestaurantDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Order>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        Guid? tableId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Order>().AsNoTracking();
        if (tableId is { } table)
        {
            query = query.Where(o => o.TableId == table);
        }

        if (status is { } state)
        {
            query = query.Where(o => o.Status == state);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(Order order) => dbContext.Set<Order>().Add(order);
}
