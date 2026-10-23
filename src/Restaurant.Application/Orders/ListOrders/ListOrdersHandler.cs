using Restaurant.Application.Abstractions;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Orders.ListOrders;

public sealed class ListOrdersHandler(IOrderRepository orders, ITableRepository tables)
    : IQueryHandler<ListOrdersQuery, PagedResponse<OrderSummaryResponse>>
{
    public async Task<PagedResponse<OrderSummaryResponse>> HandleAsync(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await orders.ListAsync(query.TableId, query.Status, query.Page, query.PageSize, cancellationToken);

        var tableIds = items.Select(o => o.TableId).Distinct().ToList();
        var numbers = (await tables.GetByIdsAsync(tableIds, cancellationToken)).ToDictionary(t => t.Id, t => t.Number);

        var summaries = items.Select(o => OrderSummaryResponse.From(o, numbers[o.TableId])).ToList();
        return PagedResponse<OrderSummaryResponse>.Create(summaries, query.Page, query.PageSize, totalCount);
    }
}
