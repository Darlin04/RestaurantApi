using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Ingredients.DeleteIngredient;

namespace Restaurant.Api.Endpoints.Ingredients;

public sealed class DeleteIngredientEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/ingredients/{id:guid}", async (
                Guid id,
                ICommandHandler<DeleteIngredientCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(new DeleteIngredientCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteIngredient")
            .WithTags("Ingredients")
            .WithSummary("Elimina un ingrediente que no está en uso")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
