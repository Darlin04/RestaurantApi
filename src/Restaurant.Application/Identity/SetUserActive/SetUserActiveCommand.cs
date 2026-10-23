namespace Restaurant.Application.Identity.SetUserActive;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive);
