using Restaurant.Domain.Common;

namespace Restaurant.Domain.Tables;

public static class TableErrors
{
    public static DomainException InvalidNumber() =>
        new("Table.InvalidNumber", "El número de mesa debe estar entre 1 y 999.");

    public static DomainException InvalidCapacity() =>
        new("Table.InvalidCapacity", "La capacidad debe estar entre 1 y 20.");

    public static DomainException InvalidDescription() =>
        new("Table.InvalidDescription", "La descripción no puede exceder 100 caracteres.");

    public static DomainException NotAvailable() =>
        new("Table.NotAvailable", "Solo se puede modificar una mesa disponible.");

    public static DomainException InvalidTransition(int number, TableStatus from, TableStatus to) =>
        new("Table.InvalidTransition", $"La mesa {number} no puede pasar de {from} a {to}.");

    public static ConflictException DuplicateNumber(int number) =>
        new("Table.DuplicateNumber", $"Ya existe la mesa número {number}.");

    public static NotFoundException NotFound(Guid id) =>
        new("Table.NotFound", $"No existe la mesa '{id}'.");
}
