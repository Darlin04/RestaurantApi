namespace Restaurant.Application.Abstractions;

public sealed class UnauthorizedException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
