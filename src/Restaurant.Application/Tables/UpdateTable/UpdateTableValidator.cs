using FluentValidation;

namespace Restaurant.Application.Tables.UpdateTable;

public sealed class UpdateTableValidator : AbstractValidator<UpdateTableCommand>
{
    public UpdateTableValidator()
    {
        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 20).WithMessage("La capacidad debe estar entre 1 y 20.");

        RuleFor(x => x.Description)
            .MaximumLength(100).WithMessage("La descripción no puede exceder 100 caracteres.");
    }
}
