using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.GetTable;

namespace Restaurant.Api.Endpoints.Tables;

public sealed class GetTableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/tables/{id:guid}", async (
                Guid id,
                IQueryHandler<GetTableQuery, TableResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetTableQuery(id), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("GetTable")
            .WithTags("Tables")
            .WithSummary("Consulta una mesa")
            .Produces<TableResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
