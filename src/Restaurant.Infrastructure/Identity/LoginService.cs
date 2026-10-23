using Microsoft.AspNetCore.Identity;

using Restaurant.Application.Abstractions;
using Restaurant.Application.Identity;
using Restaurant.Domain.Common;

namespace Restaurant.Infrastructure.Identity;

public sealed class LoginService(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    JwtTokenGenerator tokens) : ILoginService
{
    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Auth.InvalidCredentials", "Credenciales inválidas.");
        }

        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new UnauthorizedException("Auth.InvalidCredentials", "Credenciales inválidas.");
        }

        var roles = await users.GetRolesAsync(user);
        var (token, expiresAt) = tokens.Generate(user, roles);
        return new LoginResponse(token, expiresAt, user.Id, user.Email!, roles.ToList());
    }

    public async Task<CurrentUserResponse> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new NotFoundException("User.NotFound", $"No existe el usuario '{userId}'.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Auth.InvalidCredentials", "Credenciales inválidas.");
        }

        var roles = await users.GetRolesAsync(user);
        return new CurrentUserResponse(user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList());
    }
}
