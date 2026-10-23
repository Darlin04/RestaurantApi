using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.GetUser;

public sealed class GetUserHandler(IUserManagementService users) : IQueryHandler<GetUserQuery, UserResponse>
{
    public Task<UserResponse> HandleAsync(GetUserQuery query, CancellationToken cancellationToken) =>
        users.GetAsync(query.Id, cancellationToken);
}
