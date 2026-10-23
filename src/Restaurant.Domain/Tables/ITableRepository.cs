namespace Restaurant.Domain.Tables;

public interface ITableRepository
{
    Task<Table?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Table>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<Table>> ListAsync(TableStatus? status, CancellationToken cancellationToken);

    Task<bool> ExistsByNumberAsync(int number, CancellationToken cancellationToken);

    void Add(Table table);
}
