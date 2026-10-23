using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes;
using Restaurant.Application.Dishes.GetDish;

namespace Restaurant.Api.Endpoints.Dishes;

public sealed class GetDishEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/dishes/{id:guid}", async (
                Guid id,
                IQueryHandler<GetDishQuery, DishResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetDishQuery(id), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("GetDish")
            .WithTags("Dishes")
            .WithSummary("Consulta un plato con sus ingredientes")
            .Produces<DishResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
