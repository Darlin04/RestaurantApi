using FluentValidation;

namespace Restaurant.Application.Dishes.CreateDish;

public sealed class CreateDishValidator : AbstractValidator<CreateDishCommand>
{
    public CreateDishValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(80).WithMessage("El nombre no puede exceder 80 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor que 0.");

        RuleFor(x => x.Servings)
            .InclusiveBetween(1, 20).WithMessage("Las porciones deben estar entre 1 y 20.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("La categoría no es válida.");

        RuleFor(x => x.IngredientIds)
            .NotEmpty().WithMessage("Debes indicar al menos un ingrediente.");

        RuleForEach(x => x.IngredientIds)
            .NotEqual(Guid.Empty).WithMessage("El identificador del ingrediente no es válido.");
    }
}
