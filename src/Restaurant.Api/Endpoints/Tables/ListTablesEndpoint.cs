using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.ListTables;

namespace Restaurant.Api.Endpoints.Tables;

public sealed class ListTablesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/tables", async (
                [AsParameters] ListTablesQuery query,
                IQueryHandler<ListTablesQuery, IReadOnlyList<TableResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("ListTables")
            .WithTags("Tables")
            .WithSummary("Lista mesas, con filtro opcional por estado")
            .Produces<IReadOnlyList<TableResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
