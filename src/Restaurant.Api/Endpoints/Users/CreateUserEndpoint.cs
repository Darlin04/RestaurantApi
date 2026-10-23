using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Application.Identity.CreateUser;

namespace Restaurant.Api.Endpoints.Users;

public sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/users", async (
                CreateUserCommand command,
                ICommandHandler<CreateUserCommand, UserResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Created($"/api/v1/users/{result.Id}", result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateUser")
            .WithTags("Users")
            .WithSummary("Crea un usuario")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
