namespace Restaurant.Application.Identity.CreateUser;

public sealed record CreateUserCommand(string Email, string FirstName, string LastName, string Password, string Role);
