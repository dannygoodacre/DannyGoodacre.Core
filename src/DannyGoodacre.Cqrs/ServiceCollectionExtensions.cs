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
//
// An open generic validator V<T> : IValidator<T> is registered as an open generic, so the container closes it per
// request and leaves it out for a request that does not meet its constraints. Any other open generic validator, and
// every open generic handler or hook, throws: a handler must be closed to be decorated, the hook checks need the closed
// command type, and skipping them would leave code that never runs. Abstract types are not scanned, so an abstract
// generic base class works through the closed classes derived from it.

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
    /// A type implements both <see cref="IQuery{TResult}"/> and a state command marker; a non-abstract open generic
    /// type is a handler, a post-commit hook, or a validator whose only type parameter is not the request type; a
    /// post-commit hook targets
    /// a command that is not a state command; or a post-commit hook has no registered handler with a matching
    /// signature.
    /// </exception>
    // ReSharper disable once MoveToExtensionBlock - The <c>params</c> array caused a nullability warning
    // (CS8620) in callers in other assemblies, although the arguments are not null.
    public static IServiceCollection AddCqrsHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        ScannedType[] types =
        [
            .. assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsInterface: false, IsAbstract: false })
                .Select(t => new ScannedType(t, t.GetInterfaces())),
        ];

        IServiceCollection.ValidateRequestMarkerExclusion(types);

        IServiceCollection.ValidateOpenGenericTypes(types);

        services.TryAddScoped<PostCommitScope>();

        services.RegisterValidators(types);

        // Decorate only the handlers that this call adds. Then a second call does not wrap
        // the handlers of the first call again.
        var firstHandlerIndex = services.Count;

        services.AddHandlers(IsCommandHandlerInterface, types);
        services.AddHandlers(IsQueryHandlerInterface, types);

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

        services.RegisterPostCommitHooks(types);

        return services;
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddHandlers(Func<Type, bool> interfacePredicate, ScannedType[] types)
        {
            foreach (ScannedType handler in types.Where(t => t.Type.IsClass))
            {
                foreach (Type interfaceType in handler.Interfaces.Where(interfacePredicate))
                {
                    services.AddScoped(interfaceType, handler.Type);
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

        private void RegisterValidators(ScannedType[] types)
        {
            var validatorTypes = types
                .Where(t => t.Type.IsClass)
                .SelectMany(t => t.Interfaces, (impl, iface) => new { Impl = impl.Type, Iface = iface })
                .Where(x => x.Iface.IsGenericType && x.Iface.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var reg in validatorTypes)
            {
                services.TryAddEnumerable(!reg.Impl.ContainsGenericParameters
                    ? ServiceDescriptor.Scoped(reg.Iface, reg.Impl)
                    : ServiceDescriptor.Scoped(typeof(IValidator<>), reg.Impl));
            }
        }

        private void RegisterPostCommitHooks(ScannedType[] types)
        {
            var hookGenericDefinitions = new[] { typeof(IPostCommitHook<>), typeof(IPostCommitHook<,>) };

            var hookImplementations = types
                .Where(t => t.Type.IsClass)
                .SelectMany(t => t.Interfaces, (impl, iface) => new { Impl = impl.Type, Iface = iface })
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

        private static void ValidateOpenGenericTypes(ScannedType[] types)
        {
            var invalidTypes = types
                .Where(t => t.Type is { IsClass: true, ContainsGenericParameters: true })
                .Where(t => t.Interfaces.Any(i => !IsRegistrableOpenGenericInterface(t.Type, i)))
                .Select(t => FormatTypeName(t.Type))
                .ToList();

            if (invalidTypes.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The following open generic types cannot be registered, so they would never run: {string.Join(", ", invalidTypes)}. " +
                    $"Handlers and post-commit hooks must be closed types, and an open generic validator must have one type parameter " +
                    $"that is the request type, as in V<TRequest> : IValidator<TRequest>. Make the type abstract and derive closed types from it.");
            }
        }

        private static void ValidateRequestMarkerExclusion(ScannedType[] types)
        {
            var invalidRequests = types
                .Where(t => IsQueryRequest(t.Interfaces) && IsStateCommand(t.Type))
                .Select(t => t.Type)
                .ToList();

            if (invalidRequests.Count <= 0)
            {
                return;
            }
            
            var invalidNames = string.Join(", ", invalidRequests.Select(t => t.Name));
                
            throw new InvalidOperationException(
                $"Architectural Violation: The following requests implement both IQuery and command state markers ({invalidNames}). Queries must be read-only.");
        }
    }

    // Private, so only the decorators can resolve a bare handler.
    private sealed class InnerHandlerKey;

    private readonly record struct ScannedType(Type Type, Type[] Interfaces);

    // The container closes an open generic by passing the service's type arguments to the implementation in order,
    // so only a validator of the form V<T> : IValidator<T> can be registered open.
    private static bool IsRegistrableOpenGenericInterface(Type implementation, Type interfaceType)
    {
        if (!interfaceType.IsGenericType)
        {
            return true;
        }

        Type definition = interfaceType.GetGenericTypeDefinition();

        if (definition == typeof(IValidator<>))
        {
            return implementation.GetGenericArguments() is [var typeParameter]
                && interfaceType.GetGenericArguments()[0] == typeParameter;
        }

        return !IsCommandHandlerInterface(interfaceType)
            && !IsQueryHandlerInterface(interfaceType)
            && definition != typeof(IPostCommitHook<>)
            && definition != typeof(IPostCommitHook<,>);
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

    private static Type GetRequiredHandlerInterface(Type hookInterface)
    {
        Type[] genericArgs = hookInterface.GetGenericArguments();

        return hookInterface.GetGenericTypeDefinition() == typeof(IPostCommitHook<>)
            ? typeof(ICommandHandler<>).MakeGenericType(genericArgs)
            : typeof(ICommandHandler<,>).MakeGenericType(genericArgs);
    }

    // For error messages: ICommandHandler<X, Guid> rather than ICommandHandler`2.
    private static string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        // A type nested in a generic type is generic without declaring its own type parameters, so its name has no `.
        int arityIndex = type.Name.IndexOf('`');

        string name = arityIndex < 0 
            ? type.Name 
            : type.Name[..arityIndex];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatTypeName))}>";
    }

    private static bool IsQueryHandlerInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);

    private static bool IsStateCommand(Type type)
        => typeof(IStateCommand).IsAssignableFrom(type) || typeof(ITransactedStateCommand).IsAssignableFrom(type);

    private static bool IsQueryRequest(Type[] interfaces)
        => interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
}
