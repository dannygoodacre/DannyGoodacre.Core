using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DannyGoodacre.Cqrs;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans assemblies to register CQRS command and query handlers, validators, and post-commit hooks,
    /// and decorates handlers with state persistence, validation, and logging pipelines.
    /// </summary>
    /// <param name="services">The service collection to add the CQRS services to.</param>
    /// <param name="assemblies">The assemblies to scan for CQRS types.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// A classic extension method, not an extension block member. As an extension block member, the <c>params</c> array
    /// caused a nullability warning (CS8620) in callers in other assemblies, although the arguments are not null.
    /// </remarks>
    public static IServiceCollection AddCqrsHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.ValidateRequestMarkerExclusion(assemblies);

        services.TryAddScoped<PostCommitScope>();

        services.RegisterValidators(assemblies);

        // Decorate only the handlers that this call adds. Then a second call does not wrap
        // the handlers of the first call again.
        int firstHandlerIndex = services.Count;

        services.AddHandlers(IsCommandHandlerInterface, assemblies);
        services.AddHandlers(IsQueryHandlerInterface, assemblies);

        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<>), typeof(StateCommandDecorator<>));
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<,>), typeof(StateCommandDecorator<,>));

        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<>), typeof(TransactedStateCommandDecorator<>));
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<,>), typeof(TransactedStateCommandDecorator<,>));

        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<>), typeof(CommandValidationDecorator<>));
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));

        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<>), typeof(CommandLoggingDecorator<>));
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<,>), typeof(CommandLoggingDecorator<,>));
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(IQueryHandler<,>), typeof(QueryLoggingDecorator<,>));

        services.RegisterPostCommitHooks(assemblies);

        return services;
    }

    extension(IServiceCollection services)
    {
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

        private IServiceCollection DecorateOpenGeneric(int firstIndex, Type openServiceType, Type openDecoratorType)
        {
            for (int i = firstIndex; i < services.Count; i++)
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

            // TryAddEnumerable keeps all validators of a request, but adds each implementation one time only.
            foreach (var reg in validatorTypes)
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(reg.Iface, reg.Impl));
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

            var invalidHooks = hookImplementations
                .Where(x => !IsStateCommand(x.Iface.GetGenericArguments()[0]))
                .Select(x => x.Impl.Name)
                .Distinct()
                .ToList();

            if (invalidHooks.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The following post-commit hooks target commands that do not save state ({string.Join(", ", invalidHooks)}). " +
                    $"A post-commit hook runs only for an {nameof(IStateCommand)} or an {nameof(ITransactedStateCommand)}.");
            }

            // TryAddEnumerable keeps all hooks of a command, but adds each implementation one time only.
            foreach (var reg in hookImplementations)
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(reg.Iface, reg.Impl));
            }
        }

        private void ValidateRequestMarkerExclusion(Assembly[] assemblies)
        {
            // Check classes and value types, for example a record struct.
            var invalidRequests = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsInterface: false, IsAbstract: false })
                .Where(t => IsQueryRequest(t) && IsStateCommand(t))
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

    private static bool IsStateCommand(Type type)
        => typeof(IStateCommand).IsAssignableFrom(type) || typeof(ITransactedStateCommand).IsAssignableFrom(type);

    private static bool IsQueryRequest(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
}
