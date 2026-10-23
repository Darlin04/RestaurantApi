using FluentValidation;

using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos 10 caracteres e incluir mayúscula, minúscula, número y símbolo.")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos 10 caracteres e incluir mayúscula, minúscula, número y símbolo.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos 10 caracteres e incluir mayúscula, minúscula, número y símbolo.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe tener al menos 10 caracteres e incluir mayúscula, minúscula, número y símbolo.");
        RuleFor(x => x.Role)
            .Must(r => r == Roles.Admin || r == Roles.Waiter)
            .WithMessage("El rol debe ser Admin o Waiter.");
    }
}
