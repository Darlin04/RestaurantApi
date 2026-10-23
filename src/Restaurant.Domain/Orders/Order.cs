using Restaurant.Domain.Common;

namespace Restaurant.Domain.Orders;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Guid TableId { get; private set; }

    public Guid WaiterId { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public Money Subtotal { get; private set; } = Money.Zero;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static Order Open(Guid tableId, Guid waiterId, IReadOnlyList<OrderLine> lines, DateTimeOffset now)
    {
        if (lines.Count == 0)
        {
            throw OrderErrors.ItemsRequired();
        }

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            TableId = tableId,
            WaiterId = waiterId,
            Status = OrderStatus.InProgress,
            CreatedAt = now,
        };

        foreach (var line in lines)
        {
            order.AddLine(line);
        }

        order.RecalculateSubtotal();
        return order;
    }

    public void AddItem(OrderLine line)
    {
        EnsureInProgress();
        AddLine(line);
        RecalculateSubtotal();
    }

    public void RemoveItem(Guid orderItemId)
    {
        EnsureInProgress();
        var item = _items.FirstOrDefault(i => i.Id == orderItemId)
            ?? throw OrderErrors.ItemNotFound(orderItemId);

        if (_items.Count == 1)
        {
            throw OrderErrors.CannotRemoveLastItem();
        }

        _items.Remove(item);
        RecalculateSubtotal();
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureInProgress();
        Status = OrderStatus.Completed;
        CompletedAt = now;
    }

    private void AddLine(OrderLine line)
    {
        var existing = _items.FirstOrDefault(i => i.DishId == line.DishId && i.UnitPrice == line.UnitPrice);
        if (existing is null)
        {
            _items.Add(OrderItem.Create(line.DishId, line.DishName, line.UnitPrice, line.Quantity));
            return;
        }

        existing.IncreaseQuantity(line.Quantity);
    }

    private void RecalculateSubtotal() =>
        Subtotal = _items.Aggregate(Money.Zero, (total, item) => total + item.LineTotal);

    private void EnsureInProgress()
    {
        if (Status != OrderStatus.InProgress)
        {
            throw OrderErrors.NotInProgress();
        }
    }
}
