using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.CreateUser;

public sealed class CreateUserHandler(IUserManagementService users) : ICommandHandler<CreateUserCommand, UserResponse>
{
    public Task<UserResponse> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken) =>
        users.CreateAsync(command, cancellationToken);
}
