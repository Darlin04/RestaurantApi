using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes;
using Restaurant.Application.Dishes.UpdateDish;
using Restaurant.Domain.Dishes;

namespace Restaurant.Api.Endpoints.Dishes;

public sealed class UpdateDishEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/dishes/{id:guid}", async (
                Guid id,
                UpdateDishRequest request,
                ICommandHandler<UpdateDishCommand, DishResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateDishCommand(id, request.Name, request.Price, request.Servings, request.Category, request.IngredientIds);
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("UpdateDish")
            .WithTags("Dishes")
            .WithSummary("Actualiza un plato")
            .Produces<DishResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    public sealed record UpdateDishRequest(
        string Name,
        decimal Price,
        int Servings,
        DishCategory Category,
        IReadOnlyList<Guid> IngredientIds);
}
