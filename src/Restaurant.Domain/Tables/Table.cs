using Restaurant.Domain.Common;

namespace Restaurant.Domain.Tables;

public sealed class Table : AggregateRoot
{
    private const int MaxDescriptionLength = 100;

    private Table()
    {
    }

    public int Number { get; private set; }

    public int Capacity { get; private set; }

    public string? Description { get; private set; }

    public TableStatus Status { get; private set; }

    public static Table Create(int number, int capacity, string? description)
    {
        ValidateNumber(number);
        ValidateCapacity(capacity);

        return new Table
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Capacity = capacity,
            Description = NormalizeDescription(description),
            Status = TableStatus.Available,
        };
    }

    public void UpdateDetails(int capacity, string? description)
    {
        if (Status != TableStatus.Available)
        {
            throw TableErrors.NotAvailable();
        }

        ValidateCapacity(capacity);
        Capacity = capacity;
        Description = NormalizeDescription(description);
    }

    public void StartService() => ChangeStatus(TableStatus.Available, TableStatus.InService);

    public void MarkServed() => ChangeStatus(TableStatus.InService, TableStatus.Served);

    public void Release() => ChangeStatus(TableStatus.Served, TableStatus.Available);

    private static void ValidateNumber(int number)
    {
        if (number is < 1 or > 999)
        {
            throw TableErrors.InvalidNumber();
        }
    }

    private static void ValidateCapacity(int capacity)
    {
        if (capacity is < 1 or > 20)
        {
            throw TableErrors.InvalidCapacity();
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
        {
            throw TableErrors.InvalidDescription();
        }

        return trimmed;
    }

    private void ChangeStatus(TableStatus expected, TableStatus next)
    {
        if (Status != expected)
        {
            throw TableErrors.InvalidTransition(Number, Status, next);
        }

        Status = next;
    }
}
