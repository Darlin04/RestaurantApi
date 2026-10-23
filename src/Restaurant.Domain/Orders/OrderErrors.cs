using Restaurant.Domain.Common;

namespace Restaurant.Domain.Orders;

public static class OrderErrors
{
    public static DomainException ItemsRequired() =>
        new("Order.ItemsRequired", "La orden debe tener al menos un plato.");

    public static DomainException InvalidQuantity() =>
        new("Order.InvalidQuantity", "La cantidad de cada plato debe estar entre 1 y 50.");

    public static DomainException NotInProgress() =>
        new("Order.NotInProgress", "La orden ya fue completada y no se puede modificar.");

    public static DomainException ItemNotFound(Guid itemId) =>
        new("Order.ItemNotFound", $"La orden no contiene el item '{itemId}'.");

    public static DomainException CannotRemoveLastItem() =>
        new("Order.CannotRemoveLastItem", "No se puede eliminar el único plato de la orden.");

    public static NotFoundException NotFound(Guid id) =>
        new("Order.NotFound", $"No existe la orden '{id}'.");
}
