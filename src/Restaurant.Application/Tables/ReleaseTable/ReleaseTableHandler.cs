using Restaurant.Application.Abstractions;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Tables.ReleaseTable;

public sealed class ReleaseTableHandler(ITableRepository tables, IUnitOfWork unitOfWork)
    : ICommandHandler<ReleaseTableCommand, TableResponse>
{
    public async Task<TableResponse> HandleAsync(ReleaseTableCommand command, CancellationToken cancellationToken)
    {
        var table = await tables.GetByIdAsync(command.Id, cancellationToken)
            ?? throw TableErrors.NotFound(command.Id);

        table.Release();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TableResponse.From(table);
    }
}
