using Restaurant.Domain.Common;

namespace Restaurant.Domain.Orders;

public sealed class OrderItem : Entity
{
    private const int MinQuantity = 1;
    private const int MaxQuantity = 50;

    private OrderItem()
    {
    }

    public Guid DishId { get; private set; }

    public string DishName { get; private set; } = string.Empty;

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    internal static OrderItem Create(Guid dishId, string dishName, Money unitPrice, int quantity)
    {
        ValidateQuantity(quantity);
        return new OrderItem
        {
            Id = Guid.CreateVersion7(),
            DishId = dishId,
            DishName = dishName,
            UnitPrice = unitPrice,
            Quantity = quantity,
        };
    }

    internal void IncreaseQuantity(int amount)
    {
        ValidateQuantity(Quantity + amount);
        Quantity += amount;
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity is < MinQuantity or > MaxQuantity)
        {
            throw OrderErrors.InvalidQuantity();
        }
    }
}
