using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders;
using Restaurant.Application.Orders.AddOrderItem;

namespace Restaurant.Api.Endpoints.Orders;

public sealed class AddOrderItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/orders/{id:guid}/items", async (
                Guid id,
                AddOrderItemRequest request,
                ICommandHandler<AddOrderItemCommand, OrderResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new AddOrderItemCommand(id, request.DishId, request.Quantity), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Waiter)
            .WithName("AddOrderItem")
            .WithTags("Orders")
            .WithSummary("Agrega un plato a una orden en curso")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    public sealed record AddOrderItemRequest(Guid DishId, int Quantity);
}
