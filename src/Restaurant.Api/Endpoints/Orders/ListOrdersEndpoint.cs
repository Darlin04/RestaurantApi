using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders;
using Restaurant.Application.Orders.ListOrders;

namespace Restaurant.Api.Endpoints.Orders;

public sealed class ListOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/orders", async (
                [AsParameters] ListOrdersQuery query,
                IQueryHandler<ListOrdersQuery, PagedResponse<OrderSummaryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("ListOrders")
            .WithTags("Orders")
            .WithSummary("Lista órdenes paginadas, con filtros opcionales por mesa y estado")
            .Produces<PagedResponse<OrderSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
