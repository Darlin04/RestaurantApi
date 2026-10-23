using Restaurant.Domain.Orders;

namespace Restaurant.Application.Orders;

public sealed record OrderSummaryResponse(
    Guid Id,
    Guid TableId,
    int TableNumber,
    string Status,
    decimal Subtotal,
    int ItemCount,
    DateTimeOffset CreatedAt)
{
    public static OrderSummaryResponse From(Order order, int tableNumber) =>
        new(
            order.Id,
            order.TableId,
            tableNumber,
            order.Status.ToString(),
            order.Subtotal.Amount,
            order.Items.Count,
            order.CreatedAt);
}
