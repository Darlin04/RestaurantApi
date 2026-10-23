using System.Text.Json;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Domain.Common;

namespace Restaurant.Api.IntegrationTests.Platform;

[TestFixture]
public sealed class GlobalExceptionHandlerTests
{
    private static (HttpContext Context, MemoryStream Body) CreateContext()
    {
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;
        return (context, body);
    }

    private static async Task<Dictionary<string, JsonElement>> ReadProblemAsync(HttpContext context, MemoryStream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(body);
        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    [Test]
    public async Task Handle_ValidationException_Returns400()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();
        var exception = new ValidationException(new[] { new ValidationFailure("Email", "El correo no es válido.") });

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(400));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Validation.Failed"));
        Assert.That(problem["title"].GetString(), Is.EqualTo("Solicitud inválida"));
        Assert.That(problem["detail"].GetString(), Is.EqualTo("Uno o más campos no son válidos."));
        Assert.That(problem.ContainsKey("traceId"), Is.True);
    }

    [Test]
    public async Task Handle_BadHttpRequestException_Returns400()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new BadHttpRequestException("bad json"), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(400));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Request.Malformed"));
    }

    [Test]
    public async Task Handle_NotFoundException_Returns404()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new NotFoundException("Ingredient.NotFound", "No existe el ingrediente 'x'."), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(404));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Ingredient.NotFound"));
        Assert.That(problem["title"].GetString(), Is.EqualTo("Recurso no encontrado"));
    }

    [Test]
    public async Task Handle_ConflictException_Returns409()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new ConflictException("Table.DuplicateNumber", "Ya existe la mesa número 3."), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(409));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Table.DuplicateNumber"));
    }

    [Test]
    public async Task Handle_DbUpdateConcurrencyException_Returns409()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new DbUpdateConcurrencyException("concurrency"), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(409));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Concurrency.Conflict"));
    }

    [Test]
    public async Task Handle_DomainException_Returns422()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new DomainException("Table.InvalidTransition", "La mesa 3 no puede pasar de Available a Served."), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(422));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Table.InvalidTransition"));
        Assert.That(problem["title"].GetString(), Is.EqualTo("Regla de negocio incumplida"));
    }

    [Test]
    public async Task Handle_UnauthorizedException_Returns401()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new UnauthorizedException("Auth.InvalidCredentials", "Credenciales inválidas."), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(401));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Auth.InvalidCredentials"));
    }

    [Test]
    public async Task Handle_ForbiddenException_Returns403()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new ForbiddenException("Auth.Forbidden", "Acceso denegado."), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Handle_UnexpectedException_Returns500()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var (context, body) = CreateContext();

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
        var problem = await ReadProblemAsync(context, body);

        // Assert
        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(500));
        Assert.That(problem["code"].GetString(), Is.EqualTo("Server.Unexpected"));
        Assert.That(problem["detail"].GetString(), Is.EqualTo("Ocurrió un error inesperado."));
    }
}
