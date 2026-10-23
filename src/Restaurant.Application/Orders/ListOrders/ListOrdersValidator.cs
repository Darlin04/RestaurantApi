using FluentValidation;

using Restaurant.Application.Abstractions;

namespace Restaurant.Application.Orders.ListOrders;

public sealed class ListOrdersValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersValidator() => this.ApplyPaging();
}
