using Restaurant.Application.Abstractions;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Tables.GetTable;

public sealed class GetTableHandler(ITableRepository tables) : IQueryHandler<GetTableQuery, TableResponse>
{
    public async Task<TableResponse> HandleAsync(GetTableQuery query, CancellationToken cancellationToken)
    {
        var table = await tables.GetByIdAsync(query.Id, cancellationToken)
            ?? throw TableErrors.NotFound(query.Id);

        return TableResponse.From(table);
    }
}
