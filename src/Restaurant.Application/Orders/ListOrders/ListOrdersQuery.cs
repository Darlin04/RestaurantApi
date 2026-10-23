using Restaurant.Application.Abstractions;
using Restaurant.Domain.Orders;

namespace Restaurant.Application.Orders.ListOrders;

public sealed record ListOrdersQuery(Guid? TableId, OrderStatus? Status, int Page = 1, int PageSize = 20) : IPagedQuery;
