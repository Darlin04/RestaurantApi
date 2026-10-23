namespace Restaurant.Application.Identity;

public interface IUserManagementService
{
    Task<UserResponse> CreateAsync(CreateUser.CreateUserCommand command, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserResponse>> ListAsync(string? role, bool? isActive, CancellationToken cancellationToken);

    Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken);
}
