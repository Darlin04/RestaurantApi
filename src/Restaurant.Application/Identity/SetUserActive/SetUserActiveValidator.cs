using FluentValidation;

namespace Restaurant.Application.Identity.SetUserActive;

public sealed class SetUserActiveValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveValidator() =>
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("El identificador del usuario es obligatorio.");
}
