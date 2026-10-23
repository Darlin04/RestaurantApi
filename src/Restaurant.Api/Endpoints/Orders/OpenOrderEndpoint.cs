using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Orders;
using Restaurant.Application.Orders.OpenOrder;

namespace Restaurant.Api.Endpoints.Orders;

public sealed class OpenOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/orders", async (
                OpenOrderCommand command,
                ICommandHandler<OpenOrderCommand, OrderResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Created($"/api/v1/orders/{result.Id}", result);
            })
            .RequireAuthorization(Policies.Waiter)
            .WithName("OpenOrder")
            .WithTags("Orders")
            .WithSummary("Abre una orden en una mesa disponible")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
