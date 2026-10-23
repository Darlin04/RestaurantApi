using FluentValidation;

namespace Restaurant.Application.Abstractions;

public interface IPagedQuery
{
    int Page { get; }
    int PageSize { get; }
}

public static class PagingRules
{
    public static void ApplyPaging<T>(this AbstractValidator<T> validator)
        where T : IPagedQuery
    {
        validator.RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page debe ser mayor o igual a 1.");

        validator.RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("pageSize debe estar entre 1 y 100.");
    }
}
