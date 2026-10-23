namespace Restaurant.Application.Orders.RemoveOrderItem;

public sealed record RemoveOrderItemCommand(Guid OrderId, Guid ItemId);
