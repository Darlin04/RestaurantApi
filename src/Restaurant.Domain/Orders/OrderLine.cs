using Restaurant.Domain.Common;

namespace Restaurant.Domain.Orders;

public sealed record OrderLine(Guid DishId, string DishName, Money UnitPrice, int Quantity);
