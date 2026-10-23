using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity.SetUserActive;

namespace Restaurant.Api.Endpoints.Users;

public sealed class ActivateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPatch("/users/{id:guid}/activate", async (
                Guid id,
                ICommandHandler<SetUserActiveCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(new SetUserActiveCommand(id, true), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("ActivateUser")
            .WithTags("Users")
            .WithSummary("Reactiva un usuario")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
