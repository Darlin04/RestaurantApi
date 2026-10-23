using Restaurant.Application.Abstractions;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Orders.RemoveOrderItem;

public sealed class RemoveOrderItemHandler(
    IOrderRepository orders,
    ITableRepository tables,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveOrderItemCommand, OrderResponse>
{
    public async Task<OrderResponse> HandleAsync(RemoveOrderItemCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw OrderErrors.NotFound(command.OrderId);

        var table = await tables.GetByIdAsync(order.TableId, cancellationToken)
            ?? throw TableErrors.NotFound(order.TableId);

        order.RemoveItem(command.ItemId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order, table.Number);
    }
}
