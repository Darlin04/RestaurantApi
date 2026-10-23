using Microsoft.AspNetCore.Identity;

using Restaurant.Application.Identity;
using Restaurant.Domain.Common;

namespace Restaurant.Infrastructure.Identity;

public sealed class UserActivationService(UserManager<ApplicationUser> users) : IUserActivationService
{
    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new NotFoundException("User.NotFound", $"No existe el usuario '{userId}'.");
        }

        user.IsActive = isActive;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
