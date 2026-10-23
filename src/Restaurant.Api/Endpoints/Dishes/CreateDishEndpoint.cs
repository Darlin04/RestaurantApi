using Restaurant.Api.Common;
using Restaurant.Application.Abstractions;
using Restaurant.Application.Dishes;
using Restaurant.Application.Dishes.CreateDish;

namespace Restaurant.Api.Endpoints.Dishes;

public sealed class CreateDishEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/dishes", async (
                CreateDishCommand command,
                ICommandHandler<CreateDishCommand, DishResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return TypedResults.Created($"/api/v1/dishes/{result.Id}", result);
            })
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateDish")
            .WithTags("Dishes")
            .WithSummary("Crea un plato")
            .Produces<DishResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
