using FluentValidation;

namespace Restaurant.Application.Ingredients.CreateIngredient;

public sealed class CreateIngredientValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientValidator() =>
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(60).WithMessage("El nombre no puede exceder 60 caracteres.");
}
