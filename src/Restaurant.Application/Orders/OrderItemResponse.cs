using Restaurant.Domain.Orders;

namespace Restaurant.Application.Orders;

public sealed record OrderItemResponse(
    Guid Id,
    Guid DishId,
    string DishName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal)
{
    public static OrderItemResponse From(OrderItem item) =>
        new(item.Id, item.DishId, item.DishName, item.UnitPrice.Amount, item.Quantity, item.LineTotal.Amount);
}
