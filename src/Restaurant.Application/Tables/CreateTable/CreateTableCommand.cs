namespace Restaurant.Application.Tables.CreateTable;

public sealed record CreateTableCommand(int Number, int Capacity, string? Description);
