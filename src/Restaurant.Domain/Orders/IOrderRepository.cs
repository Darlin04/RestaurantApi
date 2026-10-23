namespace Restaurant.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        Guid? tableId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Order order);
}
