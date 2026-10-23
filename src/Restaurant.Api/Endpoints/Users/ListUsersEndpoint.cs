using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Application.Identity.ListUsers;

namespace Restaurant.Api.Endpoints.Users;

public sealed class ListUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/users", async (
                [AsParameters] ListUsersQuery query,
                IQueryHandler<ListUsersQuery, IReadOnlyList<UserResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("ListUsers")
            .WithTags("Users")
            .WithSummary("Lista usuarios")
            .Produces<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
}
