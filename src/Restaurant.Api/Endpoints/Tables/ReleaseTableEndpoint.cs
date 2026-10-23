using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.ReleaseTable;

namespace Restaurant.Api.Endpoints.Tables;

public sealed class ReleaseTableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPatch("/tables/{id:guid}/release", async (
                Guid id,
                ICommandHandler<ReleaseTableCommand, TableResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ReleaseTableCommand(id), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("ReleaseTable")
            .WithTags("Tables")
            .WithSummary("Libera una mesa atendida")
            .Produces<TableResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
