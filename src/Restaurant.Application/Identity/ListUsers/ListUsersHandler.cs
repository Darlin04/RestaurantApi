using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.ListUsers;

public sealed class ListUsersHandler(IUserManagementService users) : IQueryHandler<ListUsersQuery, IReadOnlyList<UserResponse>>
{
    public Task<IReadOnlyList<UserResponse>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken) =>
        users.ListAsync(query.Role, query.IsActive, cancellationToken);
}
