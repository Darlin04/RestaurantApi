using System.Diagnostics;
using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

using Restaurant.Application.Abstractions;
using Restaurant.Domain.Common;

namespace Restaurant.Api.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        int statusCode;
        string code;
        string title;
        string detail;
        object? errors = null;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                code = "Validation.Failed";
                title = "Solicitud inválida";
                detail = "Uno o más campos no son válidos.";
                errors = validationException.Errors
                    .GroupBy(e => ToCamelCase(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;
            case BadHttpRequestException:
                statusCode = StatusCodes.Status400BadRequest;
                code = "Request.Malformed";
                title = "Solicitud inválida";
                detail = "La solicitud no tiene un formato válido.";
                break;
            case NotFoundException notFound:
                statusCode = StatusCodes.Status404NotFound;
                code = notFound.Code;
                title = "Recurso no encontrado";
                detail = notFound.Message;
                break;
            case ConflictException conflict:
                statusCode = StatusCodes.Status409Conflict;
                code = conflict.Code;
                title = "Conflicto";
                detail = conflict.Message;
                break;
            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                code = "Concurrency.Conflict";
                title = "Conflicto";
                detail = "El recurso fue modificado por otra operación. Intenta de nuevo.";
                break;
            case DbUpdateException dbUpdate when IsUniqueViolation(dbUpdate):
                statusCode = StatusCodes.Status409Conflict;
                code = "Conflict.Duplicate";
                title = "Conflicto";
                detail = "Ya existe un registro con esos datos.";
                break;
            case DomainException domain:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                code = domain.Code;
                title = "Regla de negocio incumplida";
                detail = domain.Message;
                break;
            case UnauthorizedException unauthorized:
                statusCode = StatusCodes.Status401Unauthorized;
                code = unauthorized.Code;
                title = "No autenticado";
                detail = unauthorized.Message;
                break;
            case ForbiddenException forbidden:
                statusCode = StatusCodes.Status403Forbidden;
                code = forbidden.Code;
                title = "Acceso denegado";
                detail = forbidden.Message;
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                code = "Server.Unexpected";
                title = "Error interno";
                detail = "Ocurrió un error inesperado.";
                logger.LogError(exception, "Unhandled exception {TraceId}", traceId);
                break;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.io/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = detail,
            ["code"] = code,
            ["traceId"] = traceId,
        };

        if (errors is not null)
        {
            payload["errors"] = errors;
        }

        await httpContext.Response.WriteAsJsonAsync(payload, cancellationToken);
        return true;
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return JsonNamingPolicy.CamelCase.ConvertName(value);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (var current = (Exception?)exception; current is not null; current = current.InnerException)
        {
            var type = current.GetType();
            var sqlState = type.GetProperty("SqlState")?.GetValue(current) as string;
            if (sqlState == "23505")
            {
                return true;
            }

            var numberProp = type.GetProperty("Number");
            if (numberProp?.GetValue(current) is int number && (number == 2601 || number == 2627))
            {
                return true;
            }
        }

        return false;
    }
}
