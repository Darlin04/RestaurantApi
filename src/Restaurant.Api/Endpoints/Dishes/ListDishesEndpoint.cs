using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes;
using Restaurant.Application.Dishes.ListDishes;

namespace Restaurant.Api.Endpoints.Dishes;

public sealed class ListDishesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/dishes", async (
                [AsParameters] ListDishesQuery query,
                IQueryHandler<ListDishesQuery, IReadOnlyList<DishResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("ListDishes")
            .WithTags("Dishes")
            .WithSummary("Lista platos, con filtros opcionales por categoría y estado")
            .Produces<IReadOnlyList<DishResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
