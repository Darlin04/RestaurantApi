using FluentValidation;

namespace Restaurant.Application.Ingredients.RenameIngredient;

public sealed class RenameIngredientValidator : AbstractValidator<RenameIngredientCommand>
{
    public RenameIngredientValidator() =>
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(60).WithMessage("El nombre no puede exceder 60 caracteres.");
}
