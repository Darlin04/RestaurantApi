using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients;
using Restaurant.Application.Ingredients.CreateIngredient;

namespace Restaurant.Api.Endpoints.Ingredients;

public sealed class CreateIngredientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/ingredients", async (
                CreateIngredientCommand command,
                ICommandHandler<CreateIngredientCommand, IngredientResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Created($"/api/v1/ingredients/{result.Id}", result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateIngredient")
            .WithTags("Ingredients")
            .WithSummary("Crea un ingrediente")
            .Produces<IngredientResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
