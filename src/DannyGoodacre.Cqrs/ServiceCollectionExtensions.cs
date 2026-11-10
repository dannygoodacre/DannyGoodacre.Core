// AddCqrsHandlers builds the CQRS pipeline at startup, so nothing is scanned or reflected over per request.
//
// It registers every handler, then wraps each registration in decorators, innermost first: state, transaction,
// validation, logging. Each pass replaces the registration with a factory around the previous one, so the order of
// the passes is the order of the pipeline. Only state commands get the state and transaction decorators, so queries
// and plain commands need no persistence units.
//
// The bare handler moves to a keyed registration under a private key. The container still creates, disposes, and
// validates it, but only the decorators can resolve it, so no caller can bypass the pipeline.
//
// Configuration mistakes that would otherwise fail silently, such as a hook that could never run, throw here instead.
// The method may be called once per module; each call wraps only its own handlers, and registers shared services once.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Provides <see cref="IServiceCollection"/> extension methods for registering the CQRS pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans assemblies to register CQRS command and query handlers, validators, and post-commit hooks,
    /// and wraps each handler in the logging and validation decorators. Handlers of
    /// <see cref="IStateCommand"/> and <see cref="ITransactedStateCommand"/> requests also get the state
    /// and transaction decorators.
    /// </summary>
    /// <param name="services">The service collection to add the CQRS services to.</param>
    /// <param name="assemblies">The assemblies to scan for CQRS types.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A type implements both <see cref="IQuery{TResult}"/> and a state command marker; a post-commit hook targets
    /// a command that is not a state command; or a post-commit hook has no registered handler with a matching
    /// signature.
    /// </exception>
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
        var firstHandlerIndex = services.Count;

        services.AddHandlers(IsCommandHandlerInterface, assemblies);
        services.AddHandlers(IsQueryHandlerInterface, assemblies);

        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<>), typeof(StateCommandDecorator<>), IsStateCommand);
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<,>), typeof(StateCommandDecorator<,>), IsStateCommand);

        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<>), typeof(TransactedStateCommandDecorator<>), IsStateCommand);
        services.DecorateOpenGeneric(firstHandlerIndex, typeof(ICommandHandler<,>), typeof(TransactedStateCommandDecorator<,>), IsStateCommand);

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

        private IServiceCollection DecorateOpenGeneric(
            int firstIndex,
            Type openServiceType,
            Type openDecoratorType,
            Func<Type, bool>? appliesToRequest = null)
        {
            for (int i = firstIndex; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                // Skip the bare handlers this loop moves to keyed registrations, or they would be wrapped twice.
                if (descriptor.IsKeyedService
                    || !descriptor.ServiceType.IsGenericType
                    || descriptor.ServiceType.GetGenericTypeDefinition() != openServiceType)
                {
                    continue;
                }

                Type closedServiceType = descriptor.ServiceType;

                Type[] genericArgs = closedServiceType.GetGenericArguments();

                if (appliesToRequest is not null && !appliesToRequest(genericArgs[0]))
                {
                    continue;
                }

                Type closedDecoratorType = openDecoratorType.MakeGenericType(genericArgs);

                Func<IServiceProvider, object> createInner = services.CreateInnerFactory(descriptor, closedServiceType);

                ObjectFactory decoratorFactory = ActivatorUtilities.CreateFactory(
                    closedDecoratorType,
                    argumentTypes: [closedServiceType]);

                services[i] = ServiceDescriptor.Describe(
                    closedServiceType,
                    provider => decoratorFactory(provider, [createInner(provider)]),
                    descriptor.Lifetime);
            }

            return services;
        }

        private Func<IServiceProvider, object> CreateInnerFactory(ServiceDescriptor descriptor, Type serviceType)
        {
            if (descriptor.ImplementationInstance is { } instance)
            {
                return _ => instance;
            }

            if (descriptor.ImplementationType is { } implementationType)
            {
                var key = new InnerHandlerKey();

                services.Add(ServiceDescriptor.DescribeKeyed(serviceType, key, implementationType, descriptor.Lifetime));

                return provider => provider.GetRequiredKeyedService(serviceType, key);
            }

            return descriptor.ImplementationFactory!;
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
                .Where(x => x.Iface.IsGenericType && hookGenericDefinitions.Contains(x.Iface.GetGenericTypeDefinition()))
                .ToList();

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

            var registeredServiceTypes = services.Select(d => d.ServiceType).ToHashSet();

            var unmatchedHooks = hookImplementations
                .Where(x => !registeredServiceTypes.Contains(GetRequiredHandlerInterface(x.Iface)))
                .Select(x => $"{x.Impl.Name} (needs {FormatTypeName(GetRequiredHandlerInterface(x.Iface))})")
                .Distinct()
                .ToList();

            if (unmatchedHooks.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The following post-commit hooks have no command handler with a matching signature, so they would never run: " +
                    $"{string.Join(", ", unmatchedHooks)}. Register the handler in this call to {nameof(AddCqrsHandlers)} or an earlier one.");
            }

            foreach (var reg in hookImplementations)
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(reg.Iface, reg.Impl));
            }
        }

        private void ValidateRequestMarkerExclusion(Assembly[] assemblies)
        {
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

    // Private, so only the decorators can resolve a bare handler.
    private sealed class InnerHandlerKey;

    private static bool IsCommandHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        Type genericTypeDef = type.GetGenericTypeDefinition();

        return genericTypeDef == typeof(ICommandHandler<>) || genericTypeDef == typeof(ICommandHandler<,>);
    }

    private static Type GetRequiredHandlerInterface(Type hookInterface)
    {
        Type[] genericArgs = hookInterface.GetGenericArguments();

        return hookInterface.GetGenericTypeDefinition() == typeof(IPostCommitHook<>)
            ? typeof(ICommandHandler<>).MakeGenericType(genericArgs)
            : typeof(ICommandHandler<,>).MakeGenericType(genericArgs);
    }

    // For error messages: ICommandHandler<X, Guid> rather than ICommandHandler`2.
    private static string FormatTypeName(Type type)
        => type.IsGenericType
            ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(FormatTypeName))}>"
            : type.Name;

    private static bool IsQueryHandlerInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);

    private static bool IsStateCommand(Type type)
        => typeof(IStateCommand).IsAssignableFrom(type) || typeof(ITransactedStateCommand).IsAssignableFrom(type);

    private static bool IsQueryRequest(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
}
