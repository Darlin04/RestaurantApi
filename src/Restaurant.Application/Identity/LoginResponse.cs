namespace Restaurant.Application.Identity;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, Guid UserId, string Email, IReadOnlyList<string> Roles);
