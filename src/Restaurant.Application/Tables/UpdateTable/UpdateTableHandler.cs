using Restaurant.Application.Abstractions;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Tables.UpdateTable;

public sealed class UpdateTableHandler(ITableRepository tables, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTableCommand, TableResponse>
{
    public async Task<TableResponse> HandleAsync(UpdateTableCommand command, CancellationToken cancellationToken)
    {
        var table = await tables.GetByIdAsync(command.Id, cancellationToken)
            ?? throw TableErrors.NotFound(command.Id);

        table.UpdateDetails(command.Capacity, command.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TableResponse.From(table);
    }
}
