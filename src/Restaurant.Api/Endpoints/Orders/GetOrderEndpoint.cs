using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders;
using Restaurant.Application.Orders.GetOrder;

namespace Restaurant.Api.Endpoints.Orders;

public sealed class GetOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/orders/{id:guid}", async (
                Guid id,
                IQueryHandler<GetOrderQuery, OrderResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetOrderQuery(id), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("GetOrder")
            .WithTags("Orders")
            .WithSummary("Consulta una orden con sus items")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
