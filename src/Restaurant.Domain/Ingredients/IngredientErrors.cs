using Restaurant.Domain.Common;

namespace Restaurant.Domain.Ingredients;

public static class IngredientErrors
{
    public static DomainException InvalidName() =>
        new("Ingredient.InvalidName", "El nombre del ingrediente debe tener entre 2 y 60 caracteres.");

    public static ConflictException DuplicateName(string name) =>
        new("Ingredient.DuplicateName", $"Ya existe un ingrediente con el nombre '{name}'.");

    public static NotFoundException NotFound(Guid id) =>
        new("Ingredient.NotFound", $"No existe el ingrediente '{id}'.");

    public static ConflictException InUse() =>
        new("Ingredient.InUse", "El ingrediente está siendo usado por uno o más platos.");
}
