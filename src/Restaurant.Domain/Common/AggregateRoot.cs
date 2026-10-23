namespace Restaurant.Domain.Common;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }
}
