using Restaurant.Domain.Common;

namespace Restaurant.Domain.Dishes;

public static class DishErrors
{
    public static DomainException InvalidName() =>
        new("Dish.InvalidName", "El nombre del plato debe tener entre 2 y 80 caracteres.");

    public static DomainException InvalidPrice() =>
        new("Dish.InvalidPrice", "El precio debe ser mayor que 0 y no exceder 999,999.99.");

    public static DomainException InvalidServings() =>
        new("Dish.InvalidServings", "Las porciones deben estar entre 1 y 20.");

    public static DomainException IngredientsRequired() =>
        new("Dish.IngredientsRequired", "El plato debe tener al menos un ingrediente.");

    public static DomainException TooManyIngredients() =>
        new("Dish.TooManyIngredients", "El plato no puede tener más de 30 ingredientes.");

    public static DomainException Inactive(string name) =>
        new("Dish.Inactive", $"El plato '{name}' no está disponible.");

    public static ConflictException DuplicateName(string name) =>
        new("Dish.DuplicateName", $"Ya existe un plato con el nombre '{name}'.");

    public static NotFoundException NotFound(Guid id) =>
        new("Dish.NotFound", $"No existe el plato '{id}'.");
}
