using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients;
using Restaurant.Application.Ingredients.RenameIngredient;

namespace Restaurant.Api.Endpoints.Ingredients;

public sealed class RenameIngredientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/ingredients/{id:guid}", async (
                Guid id,
                RenameIngredientRequest request,
                ICommandHandler<RenameIngredientCommand, IngredientResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RenameIngredientCommand(id, request.Name), cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("RenameIngredient")
            .WithTags("Ingredients")
            .WithSummary("Renombra un ingrediente")
            .Produces<IngredientResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    public sealed record RenameIngredientRequest(string Name);
}
