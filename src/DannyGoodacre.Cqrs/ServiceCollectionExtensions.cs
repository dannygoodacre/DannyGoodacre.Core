using System.Reflection;
using DannyGoodacre.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scan the specified assemblies for concrete command handler implementations and register them as scoped services.
    /// </summary>
    /// <param name="assemblies">A list of assemblies to scan for command handler implementations.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddHandlers(x => x is { IsAbstract: false, IsClass: true } && x.IsCommandHandler(), assemblies);

    /// <summary>
    /// Scan the specified assemblies for concrete query handler implementations and register them as scoped services.
    /// </summary>
    /// <param name="assemblies">A list of assemblies to scan for query handler implementations.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddQueryHandlers(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddHandlers(x => x is { IsAbstract: false, IsClass: true } && x.IsQueryHandler(), assemblies);

    private static IServiceCollection AddHandlers(this IServiceCollection services, Func<Type, bool> predicate, params Assembly[] assemblies)
    {
        IEnumerable<Type> handlerTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(predicate);

        foreach (Type handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);

            IEnumerable<Type> interfaces = handlerType.GetHandlerInterfaces();

            foreach (Type serviceType in interfaces)
            {
                services.AddScoped(serviceType, handlerType);
            }
        }

        return services;
    }

}
