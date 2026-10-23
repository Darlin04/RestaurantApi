using Restaurant.Application.Abstractions;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Orders.GetOrder;

public sealed class GetOrderHandler(IOrderRepository orders, ITableRepository tables)
    : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<OrderResponse> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(query.Id, cancellationToken)
            ?? throw OrderErrors.NotFound(query.Id);

        var table = await tables.GetByIdAsync(order.TableId, cancellationToken)
            ?? throw TableErrors.NotFound(order.TableId);

        return OrderResponse.From(order, table.Number);
    }
}
