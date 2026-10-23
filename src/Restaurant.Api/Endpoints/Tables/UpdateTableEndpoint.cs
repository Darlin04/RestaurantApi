using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.UpdateTable;

namespace Restaurant.Api.Endpoints.Tables;

public sealed class UpdateTableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/tables/{id:guid}", async (
                Guid id,
                UpdateTableRequest request,
                ICommandHandler<UpdateTableCommand, TableResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateTableCommand(id, request.Capacity, request.Description), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("UpdateTable")
            .WithTags("Tables")
            .WithSummary("Actualiza capacidad y descripción de una mesa disponible")
            .Produces<TableResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    public sealed record UpdateTableRequest(int Capacity, string? Description);
}
