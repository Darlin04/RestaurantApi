namespace Restaurant.Application.Identity;

public sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName, string Role, bool IsActive);
