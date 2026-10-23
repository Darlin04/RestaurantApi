using Restaurant.Application.Abstractions;
using Restaurant.Domain.Dishes;
using Restaurant.Domain.Orders;
using Restaurant.Domain.Tables;

namespace Restaurant.Application.Orders.AddOrderItem;

public sealed class AddOrderItemHandler(
    IOrderRepository orders,
    IDishRepository dishes,
    ITableRepository tables,
    IUnitOfWork unitOfWork) : ICommandHandler<AddOrderItemCommand, OrderResponse>
{
    public async Task<OrderResponse> HandleAsync(AddOrderItemCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw OrderErrors.NotFound(command.OrderId);

        var dish = await dishes.GetByIdAsync(command.DishId, cancellationToken)
            ?? throw DishErrors.NotFound(command.DishId);

        var table = await tables.GetByIdAsync(order.TableId, cancellationToken)
            ?? throw TableErrors.NotFound(order.TableId);

        order.AddItem(dish.ToOrderLine(command.Quantity));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order, table.Number);
    }
}
