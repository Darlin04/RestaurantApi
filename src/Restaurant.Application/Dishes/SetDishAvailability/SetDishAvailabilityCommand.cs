namespace Restaurant.Application.Dishes.SetDishAvailability;

public sealed record SetDishAvailabilityCommand(Guid Id, bool IsActive);
