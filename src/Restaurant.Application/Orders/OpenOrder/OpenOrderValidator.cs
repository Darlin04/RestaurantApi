using FluentValidation;

namespace Restaurant.Application.Orders.OpenOrder;

public sealed class OpenOrderValidator : AbstractValidator<OpenOrderCommand>
{
    public OpenOrderValidator()
    {
        RuleFor(x => x.TableId)
            .NotEqual(Guid.Empty).WithMessage("La mesa es obligatoria.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("La orden debe tener al menos un plato.")
            .Must(items => items is null || items.Count <= 30).WithMessage("La orden no puede tener más de 30 entradas.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DishId)
                .NotEqual(Guid.Empty).WithMessage("El plato es obligatorio.");

            item.RuleFor(i => i.Quantity)
                .InclusiveBetween(1, 50).WithMessage("La cantidad de cada plato debe estar entre 1 y 50.");
        });
    }
}
