namespace Restaurant.Application.Identity;

public sealed record CurrentUserResponse(Guid UserId, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);
