using FluentValidation;

namespace Restaurant.Application.Abstractions;

public sealed class ValidationDecorator<TRequest, TResult>(
    object innerHandler,
    IEnumerable<IValidator<TRequest>> validators) : ICommandHandler<TRequest, TResult>, IQueryHandler<TRequest, TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }

        return innerHandler switch
        {
            ICommandHandler<TRequest, TResult> commandHandler => await commandHandler.HandleAsync(request, cancellationToken),
            IQueryHandler<TRequest, TResult> queryHandler => await queryHandler.HandleAsync(request, cancellationToken),
            _ => throw new InvalidOperationException($"Inner handler does not implement a handler for {typeof(TRequest).Name}."),
        };
    }
}
