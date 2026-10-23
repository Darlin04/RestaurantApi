using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients;
using Restaurant.Application.Ingredients.ListIngredients;

namespace Restaurant.Api.Endpoints.Ingredients;

public sealed class ListIngredientsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/ingredients", async (
                IQueryHandler<ListIngredientsQuery, IReadOnlyList<IngredientResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListIngredientsQuery(), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("ListIngredients")
            .WithTags("Ingredients")
            .WithSummary("Lista ingredientes ordenados por nombre")
            .Produces<IReadOnlyList<IngredientResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
