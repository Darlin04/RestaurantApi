using Restaurant.Domain.Tables;

namespace Restaurant.Application.Tables;

public sealed record TableResponse(Guid Id, int Number, int Capacity, string? Description, string Status)
{
    public static TableResponse From(Table table) =>
        new(table.Id, table.Number, table.Capacity, table.Description, table.Status.ToString());
}
