using System.Reflection;

namespace Restaurant.Api.Common;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var endpoint in endpoints)
        {
            services.AddTransient(typeof(IEndpoint), endpoint);
        }

        return services;
    }

    public static RouteGroupBuilder MapEndpoints(this RouteGroupBuilder group)
    {
        var endpoints = ((IEndpointRouteBuilder)group).ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(group);
        }

        return group;
    }
}
