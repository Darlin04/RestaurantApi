using Restaurant.Domain.Common;

namespace Restaurant.Domain.Common;

public sealed record Money
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Zero => new(0);

    public static Money Create(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException("Money.Negative", "El monto no puede ser negativo.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("Money.TooManyDecimals", "El monto no puede tener más de 2 decimales.");
        }

        return new Money(amount);
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public Money Multiply(int quantity) => new(Amount * quantity);
}
