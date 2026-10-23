using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId);
