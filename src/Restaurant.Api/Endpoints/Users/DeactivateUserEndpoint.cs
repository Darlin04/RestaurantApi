using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity.SetUserActive;

namespace Restaurant.Api.Endpoints.Users;

public sealed class DeactivateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPatch("/users/{id:guid}/deactivate", async (
                Guid id,
                ICommandHandler<SetUserActiveCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(new SetUserActiveCommand(id, false), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("DeactivateUser")
            .WithTags("Users")
            .WithSummary("Desactiva un usuario")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
