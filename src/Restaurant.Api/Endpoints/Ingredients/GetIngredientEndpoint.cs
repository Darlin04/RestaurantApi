using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients;
using Restaurant.Application.Ingredients.GetIngredient;

namespace Restaurant.Api.Endpoints.Ingredients;

public sealed class GetIngredientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/ingredients/{id:guid}", async (
                Guid id,
                IQueryHandler<GetIngredientQuery, IngredientResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetIngredientQuery(id), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Staff)
            .WithName("GetIngredient")
            .WithTags("Ingredients")
            .WithSummary("Consulta un ingrediente")
            .Produces<IngredientResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
