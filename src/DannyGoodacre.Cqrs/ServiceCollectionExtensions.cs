using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Scans assemblies to register CQRS command and query handlers, validators, and post-commit hooks,
        /// and decorates handlers with state persistence, validation, and logging pipelines.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for CQRS types.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining.</returns>
        public IServiceCollection AddCqrsHandlers(params Assembly[] assemblies)
        {
            services.ValidateRequestMarkerExclusion(assemblies);

            services.RegisterValidators(assemblies);

            services.AddHandlers(IsCommandHandlerInterface, assemblies);
            services.AddHandlers(IsQueryHandlerInterface, assemblies);

            services.DecorateOpenGeneric(typeof(ICommandHandler<>), typeof(StateCommandHandlerDecorator<>));
            services.DecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(StateCommandHandlerDecorator<,>));

            services.DecorateOpenGeneric(typeof(ICommandHandler<>), typeof(CommandValidationDecorator<>));
            services.DecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
            services.DecorateOpenGeneric(typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));

            services.DecorateOpenGeneric(typeof(ICommandHandler<>), typeof(CommandLoggingDecorator<>));
            services.DecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(CommandLoggingDecorator<,>));
            services.DecorateOpenGeneric(typeof(IQueryHandler<,>), typeof(QueryLoggingDecorator<,>));

            services.RegisterPostCommitHooks(assemblies);

            return services;
        }

        private IServiceCollection AddHandlers(Func<Type, bool> interfacePredicate, params Assembly[] assemblies)
        {
            var handlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false, IsClass: true });

            foreach (Type handlerType in handlerTypes)
            {
                var targetInterfaces = handlerType.GetInterfaces().Where(interfacePredicate);

                foreach (Type interfaceType in targetInterfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }

            return services;
        }

        private IServiceCollection DecorateOpenGeneric(Type openServiceType, Type openDecoratorType)
        {
            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (!descriptor.ServiceType.IsGenericType || descriptor.ServiceType.GetGenericTypeDefinition() != openServiceType)
                {
                    continue;
                }

                Type closedServiceType = descriptor.ServiceType;

                Type[] genericArgs = closedServiceType.GetGenericArguments();

                Type closedDecoratorType = openDecoratorType.MakeGenericType(genericArgs);

                // Pre-compile the constructor factory at startup so resolving decorated handlers
                // during HTTP requests incurs zero runtime reflection overhead
                ObjectFactory decoratorFactory = ActivatorUtilities.CreateFactory(
                    closedDecoratorType,
                    argumentTypes: [closedServiceType]);

                services[i] = ServiceDescriptor.Describe(
                    closedServiceType,
                    provider =>
                    {
                        object inner = descriptor.ImplementationInstance
                                       ?? (descriptor.ImplementationType is not null
                                           ? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType)
                                           : descriptor.ImplementationFactory!(provider));

                        return decoratorFactory(provider, [inner]);
                    },
                    descriptor.Lifetime);
            }

            return services;
        }

        private void RegisterValidators(Assembly[] assemblies)
        {
            var validatorTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .SelectMany(t => t.GetInterfaces(), (impl, iface) => new { Impl = impl, Iface = iface })
                .Where(x => x.Iface.IsGenericType && x.Iface.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var reg in validatorTypes)
            {
                services.AddScoped(reg.Iface, reg.Impl);
            }
        }

        private void RegisterPostCommitHooks(Assembly[] assemblies)
        {
            var hookGenericDefinitions = new[] { typeof(IPostCommitHook<>), typeof(IPostCommitHook<,>) };

            var hookImplementations = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .SelectMany(t => t.GetInterfaces(), (impl, iface) => new { Impl = impl, Iface = iface })
                .Where(x => x.Iface.IsGenericType && hookGenericDefinitions.Contains(x.Iface.GetGenericTypeDefinition()));

            foreach (var reg in hookImplementations)
            {
                services.AddScoped(reg.Iface, reg.Impl);
            }
        }

        private void ValidateRequestMarkerExclusion(Assembly[] assemblies)
        {
            var invalidRequests = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Where(t => IsQueryRequest(t) && (typeof(IStateCommand).IsAssignableFrom(t) || typeof(ITransactedCommand).IsAssignableFrom(t)))
                .ToList();

            if (invalidRequests.Count > 0)
            {
                string invalidNames = string.Join(", ", invalidRequests.Select(t => t.Name));
                throw new InvalidOperationException(
                    $"Architectural Violation: The following requests implement both IQuery and command state markers ({invalidNames}). Queries must be read-only.");
            }
        }
    }

    private static bool IsCommandHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        Type genericTypeDef = type.GetGenericTypeDefinition();

        return genericTypeDef == typeof(ICommandHandler<>) || genericTypeDef == typeof(ICommandHandler<,>);
    }

    private static bool IsQueryHandlerInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);

    private static bool IsQueryRequest(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
}
