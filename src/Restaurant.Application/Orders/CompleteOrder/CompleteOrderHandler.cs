using Restaurant.Application.Abstractions;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Orders.CompleteOrder;

public sealed class CompleteOrderHandler(
    IOrderRepository orders,
    ITableRepository tables,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<CompleteOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> HandleAsync(CompleteOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw OrderErrors.NotFound(command.OrderId);

        var table = await tables.GetByIdAsync(order.TableId, cancellationToken)
            ?? throw TableErrors.NotFound(order.TableId);

        order.Complete(timeProvider.GetUtcNow());
        table.MarkServed();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order, table.Number);
    }
}
