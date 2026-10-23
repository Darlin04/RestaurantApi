using Restaurant.Application.Abstractions;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Tables.CreateTable;

public sealed class CreateTableHandler(ITableRepository tables, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTableCommand, TableResponse>
{
    public async Task<TableResponse> HandleAsync(CreateTableCommand command, CancellationToken cancellationToken)
    {
        var table = Table.Create(command.Number, command.Capacity, command.Description);
        if (await tables.ExistsByNumberAsync(table.Number, cancellationToken))
        {
            throw TableErrors.DuplicateNumber(table.Number);
        }

        tables.Add(table);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TableResponse.From(table);
    }
}
