using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Scan the specified assemblies for concrete command handler implementations and register them as scoped services.
        /// </summary>
        /// <param name="assemblies">A list of assemblies to scan for command handler implementations.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public IServiceCollection AddCommandHandlers(params Assembly[] assemblies)
            => services.AddHandlers(x => x is { IsAbstract: false, IsClass: true } && x.IsCommandHandler(), assemblies);

        /// <summary>
        /// Scan the specified assemblies for concrete query handler implementations and register them as scoped services.
        /// </summary>
        /// <param name="assemblies">A list of assemblies to scan for query handler implementations.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public IServiceCollection AddQueryHandlers(params Assembly[] assemblies)
            => services.AddHandlers(x => x is { IsAbstract: false, IsClass: true } && x.IsQueryHandler(), assemblies);

        private IServiceCollection AddHandlers(Func<Type, bool> predicate, params Assembly[] assemblies)
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
}
