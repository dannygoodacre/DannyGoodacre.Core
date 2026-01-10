using System.Reflection;
using DannyGoodacre.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace SystemMonitor.Core;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCommandHandlers(params Assembly[] assemblies)
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

        public IServiceCollection AddQueryHandlers(params Assembly[] assemblies)
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

}
