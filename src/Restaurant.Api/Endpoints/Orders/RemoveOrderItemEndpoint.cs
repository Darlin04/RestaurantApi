using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders;
using Restaurant.Application.Orders.RemoveOrderItem;

namespace Restaurant.Api.Endpoints.Orders;

public sealed class RemoveOrderItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/orders/{id:guid}/items/{itemId:guid}", async (
                Guid id,
                Guid itemId,
                ICommandHandler<RemoveOrderItemCommand, OrderResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RemoveOrderItemCommand(id, itemId), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Waiter)
            .WithName("RemoveOrderItem")
            .WithTags("Orders")
            .WithSummary("Quita un plato de una orden en curso")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
