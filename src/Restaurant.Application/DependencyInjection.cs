using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Restaurant.Application.Abstractions;

namespace Restaurant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        var handlerTypes = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);

        foreach (var implementation in handlerTypes)
        {
            var handlerInterfaces = implementation.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .ToList();

            if (handlerInterfaces.Count == 0)
            {
                continue;
            }

            services.AddScoped(implementation);

            foreach (var handlerInterface in handlerInterfaces)
            {
                var requestType = handlerInterface.GetGenericArguments()[0];
                var resultType = handlerInterface.GetGenericArguments()[1];
                var decoratorType = typeof(ValidationDecorator<,>).MakeGenericType(requestType, resultType);
                var validatorsType = typeof(IEnumerable<>).MakeGenericType(
                    typeof(IValidator<>).MakeGenericType(requestType));

                services.AddScoped(handlerInterface, sp =>
                {
                    var inner = sp.GetRequiredService(implementation);
                    var validators = sp.GetRequiredService(validatorsType);
                    return Activator.CreateInstance(decoratorType, inner, validators)!;
                });
            }
        }

        return services;
    }
}
