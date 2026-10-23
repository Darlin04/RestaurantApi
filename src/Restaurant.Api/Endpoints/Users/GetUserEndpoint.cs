using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Application.Identity.GetUser;

namespace Restaurant.Api.Endpoints.Users;

public sealed class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/users/{id:guid}", async (
                Guid id,
                IQueryHandler<GetUserQuery, UserResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetUserQuery(id), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("GetUser")
            .WithTags("Users")
            .WithSummary("Consulta un usuario")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
