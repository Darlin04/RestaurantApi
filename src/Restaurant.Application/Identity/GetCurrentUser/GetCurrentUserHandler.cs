using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.GetCurrentUser;

public sealed class GetCurrentUserHandler(ILoginService loginService) : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public Task<CurrentUserResponse> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken) =>
        loginService.GetCurrentAsync(query.UserId, cancellationToken);
}
