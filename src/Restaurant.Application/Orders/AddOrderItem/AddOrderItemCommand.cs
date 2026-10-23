namespace Restaurant.Application.Orders.AddOrderItem;

public sealed record AddOrderItemCommand(Guid OrderId, Guid DishId, int Quantity);
