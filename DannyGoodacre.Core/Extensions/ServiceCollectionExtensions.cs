using System.Reflection;
using DannyGoodacre.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace SystemMonitor.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x => x is { IsAbstract: false, IsClass: true } && x.IsCommandHandler());

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces();

            foreach (var serviceType in interfaces)
            {
                services.AddScoped(serviceType, handlerType);
            }
        }

        return services;
    }

    public static IServiceCollection AddQueryHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x => x is { IsAbstract: false, IsClass: true } && x.IsQueryHandler());

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces();

            foreach (var serviceType in interfaces)
            {
                services.AddScoped(serviceType, handlerType);
            }
        }

        return services;
    }
}
