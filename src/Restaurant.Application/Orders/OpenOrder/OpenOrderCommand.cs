namespace Restaurant.Application.Orders.OpenOrder;

public sealed record OpenOrderCommand(Guid TableId, IReadOnlyList<OpenOrderItem> Items);
