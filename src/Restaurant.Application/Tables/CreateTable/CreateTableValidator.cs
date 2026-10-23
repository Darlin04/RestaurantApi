using FluentValidation;

namespace Restaurant.Application.Tables.CreateTable;

public sealed class CreateTableValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableValidator()
    {
        RuleFor(x => x.Number)
            .InclusiveBetween(1, 999).WithMessage("El número de mesa debe estar entre 1 y 999.");

        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 20).WithMessage("La capacidad debe estar entre 1 y 20.");

        RuleFor(x => x.Description)
            .MaximumLength(100).WithMessage("La descripción no puede exceder 100 caracteres.");
    }
}
