using Restaurant.Application.Abstractions;
using Restaurant.Domain.Common;

namespace Restaurant.Application.Identity.SetUserActive;

public sealed class SetUserActiveHandler(IUserActivationService users, ICurrentUser currentUser)
    : ICommandHandler<SetUserActiveCommand, Unit>
{
    public async Task<Unit> HandleAsync(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsActive && command.UserId == currentUser.UserId)
        {
            throw new DomainException("User.CannotDeactivateSelf", "No puedes desactivar tu propio usuario.");
        }

        await users.SetActiveAsync(command.UserId, command.IsActive, cancellationToken);
        return Unit.Value;
    }
}
