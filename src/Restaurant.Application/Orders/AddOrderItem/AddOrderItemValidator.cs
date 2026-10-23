using FluentValidation;

namespace Restaurant.Application.Orders.AddOrderItem;

public sealed class AddOrderItemValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemValidator()
    {
        RuleFor(x => x.DishId)
            .NotEqual(Guid.Empty).WithMessage("El plato es obligatorio.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 50).WithMessage("La cantidad de cada plato debe estar entre 1 y 50.");
    }
}
