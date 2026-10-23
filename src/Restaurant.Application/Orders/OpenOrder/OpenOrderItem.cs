namespace Restaurant.Application.Orders.OpenOrder;

public sealed record OpenOrderItem(Guid DishId, int Quantity);
