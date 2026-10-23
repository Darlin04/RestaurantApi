using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Restaurant.Application.Abstractions;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Falta la configuración 'ConnectionStrings:Default'. Configúrala con dotnet user-secrets o variables de entorno.");
        }

        services.AddDbContext<RestaurantDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                o => o.MigrationsAssembly(typeof(RestaurantDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RestaurantDbContext>());

        var assembly = typeof(DependencyInjection).Assembly;
        var domainAssembly = typeof(Domain.Common.AggregateRoot).Assembly;
        var applicationAssembly = typeof(IUnitOfWork).Assembly;
        var allowedNamespaces = new[]
        {
            domainAssembly.GetName().Name!,
            applicationAssembly.GetName().Name!,
        };

        var candidates = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract &&
            (t.Name.EndsWith("Repository", StringComparison.Ordinal) ||
             t.Name.EndsWith("Service", StringComparison.Ordinal)));

        foreach (var implementation in candidates)
        {
            var interfaces = implementation.GetInterfaces()
                .Where(i => i.Assembly.GetName().Name is not null &&
                    allowedNamespaces.Contains(i.Assembly.GetName().Name))
                .ToList();

            foreach (var serviceInterface in interfaces)
            {
                services.AddScoped(serviceInterface, implementation);
            }
        }

        services.AddHealthChecks().AddDbContextCheck<RestaurantDbContext>();

        return services;
    }
}
