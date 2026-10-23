using Restaurant.Domain.Orders;

namespace Restaurant.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    Guid TableId,
    int TableNumber,
    Guid WaiterId,
    string Status,
    decimal Subtotal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse From(Order order, int tableNumber) =>
        new(
            order.Id,
            order.TableId,
            tableNumber,
            order.WaiterId,
            order.Status.ToString(),
            order.Subtotal.Amount,
            order.CreatedAt,
            order.CompletedAt,
            order.Items.Select(OrderItemResponse.From).ToList());
}
