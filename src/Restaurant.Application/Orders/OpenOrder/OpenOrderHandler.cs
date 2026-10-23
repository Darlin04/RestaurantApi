using Restaurant.Application.Abstractions;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Orders.OpenOrder;

public sealed class OpenOrderHandler(
    ITableRepository tables,
    IDishRepository dishes,
    IOrderRepository orders,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<OpenOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> HandleAsync(OpenOrderCommand command, CancellationToken cancellationToken)
    {
        var table = await tables.GetByIdAsync(command.TableId, cancellationToken)
            ?? throw TableErrors.NotFound(command.TableId);

        var dishIds = command.Items.Select(i => i.DishId).Distinct().ToList();
        var found = (await dishes.GetByIdsAsync(dishIds, cancellationToken)).ToDictionary(d => d.Id);
        var missing = dishIds.Where(id => !found.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw DishErrors.NotFound(missing[0]);
        }

        var lines = command.Items.Select(item => found[item.DishId].ToOrderLine(item.Quantity)).ToList();

        table.StartService();
        var order = Order.Open(table.Id, currentUser.UserId, lines, timeProvider.GetUtcNow());
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order, table.Number);
    }
}
