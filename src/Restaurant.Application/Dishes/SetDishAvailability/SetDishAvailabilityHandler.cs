using Restaurant.Application.Abstractions;
using Restaurant.Domain.Dishes;

namespace Restaurant.Application.Dishes.SetDishAvailability;

public sealed class SetDishAvailabilityHandler(IDishRepository dishes, IUnitOfWork unitOfWork)
    : ICommandHandler<SetDishAvailabilityCommand, Unit>
{
    public async Task<Unit> HandleAsync(SetDishAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var dish = await dishes.GetByIdAsync(command.Id, cancellationToken)
            ?? throw DishErrors.NotFound(command.Id);

        if (command.IsActive)
        {
            dish.Activate();
        }
        else
        {
            dish.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
