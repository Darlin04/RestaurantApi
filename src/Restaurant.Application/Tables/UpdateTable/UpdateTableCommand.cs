namespace Restaurant.Application.Tables.UpdateTable;

public sealed record UpdateTableCommand(Guid Id, int Capacity, string? Description);
