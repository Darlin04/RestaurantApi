using Restaurant.Application.Abstractions;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Tables.ListTables;

public sealed class ListTablesHandler(ITableRepository tables) : IQueryHandler<ListTablesQuery, IReadOnlyList<TableResponse>>
{
    public async Task<IReadOnlyList<TableResponse>> HandleAsync(ListTablesQuery query, CancellationToken cancellationToken)
    {
        var items = await tables.ListAsync(query.Status, cancellationToken);
        return items.Select(TableResponse.From).ToList();
    }
}
