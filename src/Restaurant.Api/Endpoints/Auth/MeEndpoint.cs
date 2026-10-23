using System.Security.Claims;

using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Application.Identity.GetCurrentUser;

namespace Restaurant.Api.Endpoints.Auth;

public sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/auth/me", async (
                ClaimsPrincipal user,
                IQueryHandler<GetCurrentUserQuery, CurrentUserResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var sub = user.FindFirstValue("sub");
                var query = new GetCurrentUserQuery(Guid.Parse(sub!));
                var result = await handler.HandleAsync(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .WithSummary("Obtener usuario actual")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
