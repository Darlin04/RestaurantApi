namespace Restaurant.Application.Identity.ListUsers;

public sealed record ListUsersQuery(string? Role, bool? IsActive);
