using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Application.Identity.Login;

namespace Restaurant.Api.Endpoints.Auth;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/login", async (
                LoginCommand command,
                ICommandHandler<LoginCommand, LoginResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .WithName("Login")
            .WithTags("Auth")
            .WithSummary("Iniciar sesión")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
}
