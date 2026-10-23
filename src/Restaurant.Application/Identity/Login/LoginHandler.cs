using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Identity.Login;

public sealed class LoginHandler(ILoginService loginService) : ICommandHandler<LoginCommand, LoginResponse>
{
    public Task<LoginResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken) =>
        loginService.LoginAsync(command.Email, command.Password, cancellationToken);
}
