using System.Reflection;
using DannyGoodacre.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Core;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCommandHandlers(params Assembly[] assemblies)
        {
            IEnumerable<Type> handlerTypes = assemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => x is { IsAbstract: false, IsClass: true } && x.IsCommandHandler());

            foreach (Type handlerType in handlerTypes)
            {
                _ = services.AddScoped(handlerType);

                IEnumerable<Type> interfaces = handlerType.GetHandlerInterfaces();

                foreach (Type serviceType in interfaces)
                {
                    services.AddScoped(serviceType, handlerType);
                }
            }

            return services;
        }

        public IServiceCollection AddQueryHandlers(params Assembly[] assemblies)
        {
            IEnumerable<Type> handlerTypes = assemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => x is { IsAbstract: false, IsClass: true } && x.IsQueryHandler());

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
}
