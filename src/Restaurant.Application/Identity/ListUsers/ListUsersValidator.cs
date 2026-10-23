using FluentValidation;

using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.ListUsers;

public sealed class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => r == null || r == Roles.Admin || r == Roles.Waiter)
            .WithMessage("El rol debe ser Admin o Waiter.");
    }
}
