using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity;

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<CurrentUserResponse> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
}
