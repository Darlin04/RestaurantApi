using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Tables;
using Restaurant.Application.Tables.CreateTable;

namespace Restaurant.Api.Endpoints.Tables;

public sealed class CreateTableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/tables", async (
                CreateTableCommand command,
                ICommandHandler<CreateTableCommand, TableResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Created($"/api/v1/tables/{result.Id}", result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateTable")
            .WithTags("Tables")
            .WithSummary("Crea una mesa")
            .Produces<TableResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
