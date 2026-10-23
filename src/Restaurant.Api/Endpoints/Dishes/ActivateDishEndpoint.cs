using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes.SetDishAvailability;

namespace Restaurant.Api.Endpoints.Dishes;

public sealed class ActivateDishEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPatch("/dishes/{id:guid}/activate", async (
                Guid id,
                ICommandHandler<SetDishAvailabilityCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(new SetDishAvailabilityCommand(id, true), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("ActivateDish")
            .WithTags("Dishes")
            .WithSummary("Activa un plato")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
