// AddCqrsHandlers builds the CQRS pipeline at startup, so nothing is scanned or reflected over per request.
//
// It registers every handler, then wraps each registration in decorators, innermost first: state, transaction,
// validation, logging. Each pass replaces the registration with a factory around the previous one, so the order of
// the passes is the order of the pipeline. Only state commands get the state and transaction decorators, so queries
// and plain commands need no persistence units.
//
// The bare handler moves to a keyed registration under a private key. The container still creates, disposes, and
// validates it, but only the decorators can resolve it, so no caller can bypass the pipeline. That keyed registration
// keeps the handler's type, which is how a later call recognizes the handlers of earlier calls.
//
// Configuration mistakes that would otherwise fail silently, such as a hook that could never run, throw here instead.
// Every check runs before the service collection changes, and all problems are reported in one exception, so a failed
// call leaves the collection as it was and one restart shows every mistake.
// The method may be called once per module; each call wraps only its own handlers, and registers shared services once.
// A handler, validator, or hook that an earlier call registered is not registered again.
//
// Type.GetInterfaces includes inherited interfaces, so a class derived from a concrete handler, validator, or hook is
// one too. Each request must have exactly one handler, so a second handler for the same handler interface throws,
// whatever the reason. Several validators and hooks per request are allowed, but one derived from another concrete
// one of the same interface throws, because the base would also run on its own.
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
    private static readonly Type[] HookDefinitions = [typeof(IPostCommitHook<>), typeof(IPostCommitHook<,>)];

    /// <summary>
    /// Scans assemblies to register CQRS command and query handlers, validators, and post-commit hooks,
    /// and wraps each handler in the logging and validation decorators. Handlers of
    /// <see cref="IStateCommand"/> and <see cref="ITransactedStateCommand"/> requests also get the state
    /// and transaction decorators.
    /// </summary>
    /// <remarks>
    /// A handler, validator, or hook registered by an earlier call is not registered again. All checks run before
    /// the service collection changes, and one exception reports every problem found.
    /// </remarks>
    /// <param name="services">The service collection to add the CQRS services to.</param>
    /// <param name="assemblies">The assemblies to scan for CQRS types.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A type implements both <see cref="IQuery{TResult}"/> and a state command marker; a non-abstract open generic
    /// type is a handler, a post-commit hook, or a validator whose only type parameter is not the request type; a
    /// handler interface has more than one implementation, in this call or with an earlier one; a validator or
    /// post-commit hook derives from a non-abstract one of the same interface; a post-commit hook targets a command
    /// that is not a state command; or a post-commit hook has no registered handler with a matching signature.
    /// </exception>
    // ReSharper disable once MoveToExtensionBlock - The <c>params</c> array caused a nullability warning
    // (CS8620) in callers in other assemblies, although the arguments are not null.
    public static IServiceCollection AddCqrsHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        ScannedType[] types =
        [
            .. assemblies
                .Distinct()
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsInterface: false, IsAbstract: false })
                .Select(t => new ScannedType(t, t.GetInterfaces())),
        ];

        Registration[] handlers = FindHandlers(types);
        Registration[] validators = FindValidators(types);
        Registration[] hooks = FindHooks(types);

        string[] problems =
        [
            .. FindRequestMarkerConflicts(types),
            .. FindUnregistrableOpenGenericTypes(types),
            .. services.FindDuplicateHandlers(handlers),
            .. services.FindConcreteBases(
                validators,
                [typeof(IValidator<>)],
                "validators derive from another registered validator of the same request, so the base's rules would run twice",
                "validator"),
            .. services.FindConcreteBases(
                hooks,
                HookDefinitions,
                "post-commit hooks derive from another registered hook of the same command, so the base hook would run twice",
                "hook"),
            .. FindHooksForPlainCommands(hooks),
            .. services.FindUnmatchedHooks(hooks, handlers),
        ];

        if (problems.Length > 0)
        {
            throw new InvalidOperationException(problems.Length == 1
                ? problems[0]
                : $"{nameof(AddCqrsHandlers)} found {problems.Length} problems.{Environment.NewLine}{Environment.NewLine}" +
                  string.Join(Environment.NewLine + Environment.NewLine, problems));
        }

        services.TryAddScoped<PostCommitScope>();

        foreach (Registration validator in validators)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(validator.Service, validator.Implementation));
        }

        HashSet<Registration> earlierHandlers = services.RegisteredHandlers();

        // Decorate only the handlers that this call adds. Then a second call does not wrap
        // the handlers of the first call again.
        var firstHandlerIndex = services.Count;

        foreach (Registration handler in handlers.Where(h => !earlierHandlers.Contains(h)))
        {
            services.AddScoped(handler.Service, handler.Implementation);
        }

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

        foreach (Registration hook in hooks)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(hook.Service, hook.Implementation));
        }

        return services;
    }

    extension(IServiceCollection services)
    {
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

        // The handlers of earlier calls, read from the keyed registrations that hold the bare handlers.
        private HashSet<Registration> RegisteredHandlers()
            => services
                .Where(d => d is { IsKeyedService: true, ServiceKey: InnerHandlerKey, KeyedImplementationType: not null })
                .Select(d => new Registration(d.ServiceType, d.KeyedImplementationType!))
                .ToHashSet();

        private IEnumerable<Registration> RegisteredImplementations(Type[] serviceDefinitions)
            => services
                .Where(d => !d.IsKeyedService
                    && d.ServiceType.IsGenericType
                    && serviceDefinitions.Contains(d.ServiceType.GetGenericTypeDefinition()))
                .Where(d => d.ImplementationType is not null)
                .Select(d => new Registration(d.ServiceType, d.ImplementationType!));

        private IEnumerable<string> FindDuplicateHandlers(Registration[] handlers)
        {
            var duplicates = services.RegisteredHandlers()
                .Concat(handlers)
                .Distinct()
                .GroupBy(r => r.Service)
                .Where(g => g.Count() > 1 && g.Any(handlers.Contains))
                .Select(g =>
                {
                    Type[] implementations = [.. g.Select(r => r.Implementation)];

                    var names = implementations.Select(i => DescribeWithBase(i, implementations));

                    return $"{TypeNameFormatter.Format(g.Key)} ({string.Join(", ", names)})";
                })
                .ToList();

            if (duplicates.Count > 0)
            {
                yield return
                    $"The following requests have more than one handler, so which one runs would be arbitrary: {string.Join("; ", duplicates)}. " +
                    "Each request must have one handler. To share code between handlers, move it into an abstract base class.";
            }
        }

        // A non-abstract base is registered in its own right, so it would run alongside the type derived from it.
        private IEnumerable<string> FindConcreteBases(
            Registration[] registrations,
            Type[] serviceDefinitions,
            string problem,
            string kind)
        {
            var all = services.RegisteredImplementations(serviceDefinitions).Concat(registrations).ToHashSet();

            var conflicts = new List<string>();

            foreach (Registration registration in all)
            {
                for (Type? baseType = registration.Implementation.BaseType;
                     baseType is not null && baseType != typeof(object);
                     baseType = baseType.BaseType)
                {
                    if (FindRegistration(all, registration.Service, baseType) is not { } baseRegistration)
                    {
                        continue;
                    }

                    if (registrations.Contains(registration) || registrations.Contains(baseRegistration))
                    {
                        conflicts.Add(
                            $"{TypeNameFormatter.Format(registration.Implementation)} : {TypeNameFormatter.Format(baseRegistration.Implementation)} " +
                            $"({TypeNameFormatter.Format(registration.Service)})");
                    }

                    break;
                }
            }

            if (conflicts.Count > 0)
            {
                yield return
                    $"The following {problem}: {string.Join(", ", conflicts.Distinct())}. " +
                    $"Make the base abstract to run it once as part of the derived {kind}, or remove the inheritance to run them as separate {kind}s.";
            }
        }

        private IEnumerable<string> FindUnmatchedHooks(Registration[] hooks, Registration[] handlers)
        {
            var registeredServiceTypes = services
                .Select(d => d.ServiceType)
                .Concat(handlers.Select(h => h.Service))
                .ToHashSet();

            var unmatchedHooks = hooks
                .Where(h => IsStateCommand(h.Service.GetGenericArguments()[0]))
                .Where(h => !registeredServiceTypes.Contains(GetRequiredHandlerInterface(h.Service)))
                .Select(h => $"{TypeNameFormatter.Format(h.Implementation)} (needs {TypeNameFormatter.Format(GetRequiredHandlerInterface(h.Service))})")
                .Distinct()
                .ToList();

            if (unmatchedHooks.Count > 0)
            {
                yield return
                    $"The following post-commit hooks have no command handler with a matching signature, so they would never run: " +
                    $"{string.Join(", ", unmatchedHooks)}. Register the handler in this call to {nameof(AddCqrsHandlers)} or an earlier one.";
            }
        }
    }

    // Private, so only the decorators can resolve a bare handler.
    private sealed class InnerHandlerKey;

    private readonly record struct ScannedType(Type Type, Type[] Interfaces);

    // An open generic validator's Service is IValidator<> and its Implementation is the generic type definition.
    private readonly record struct Registration(Type Service, Type Implementation);

    private static Registration[] FindHandlers(ScannedType[] types)
        =>
        [
            .. types
                .Where(t => t.Type is { IsClass: true, ContainsGenericParameters: false })
                .SelectMany(t => t.Interfaces
                    .Where(i => IsCommandHandlerInterface(i) || IsQueryHandlerInterface(i))
                    .Select(i => new Registration(i, t.Type))),
        ];

    // Open generic validators that cannot be registered are left out; FindUnregistrableOpenGenericTypes reports them.
    private static Registration[] FindValidators(ScannedType[] types)
        =>
        [
            .. types
                .Where(t => t.Type.IsClass)
                .SelectMany(t => t.Interfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .Where(i => !t.Type.ContainsGenericParameters || IsRegistrableOpenGenericInterface(t.Type, i))
                    .Select(i => t.Type.ContainsGenericParameters
                        ? new Registration(typeof(IValidator<>), t.Type)
                        : new Registration(i, t.Type))),
        ];

    private static Registration[] FindHooks(ScannedType[] types)
        =>
        [
            .. types
                .Where(t => t.Type is { IsClass: true, ContainsGenericParameters: false })
                .SelectMany(t => t.Interfaces
                    .Where(i => i.IsGenericType && HookDefinitions.Contains(i.GetGenericTypeDefinition()))
                    .Select(i => new Registration(i, t.Type))),
        ];

    // An open generic validator is registered under IValidator<> as its generic type definition, so a validator derived
    // from one, open or closed, finds it by those.
    private static Registration? FindRegistration(HashSet<Registration> registrations, Type service, Type baseType)
    {
        Registration[] candidates =
        [
            new(service, OpenIfGeneric(baseType)),
            .. service.IsConstructedGenericType && baseType.IsGenericType
                ? [new Registration(service.GetGenericTypeDefinition(), baseType.GetGenericTypeDefinition())]
                : Array.Empty<Registration>(),
        ];

        return candidates.Where(registrations.Contains).Cast<Registration?>().FirstOrDefault();
    }

    // Names the nearest base class in the same list, so the message shows why a class counts as a handler.
    private static string DescribeWithBase(Type implementation, Type[] implementations)
    {
        for (Type? baseType = implementation.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (implementations.Contains(baseType))
            {
                return $"{TypeNameFormatter.Format(implementation)} : {TypeNameFormatter.Format(baseType)}";
            }
        }

        return TypeNameFormatter.Format(implementation);
    }

    private static Type OpenIfGeneric(Type type)
        => type is { IsGenericType: true, ContainsGenericParameters: true } ? type.GetGenericTypeDefinition() : type;

    private static IEnumerable<string> FindRequestMarkerConflicts(ScannedType[] types)
    {
        var invalidRequests = types
            .Where(t => IsQueryRequest(t.Interfaces) && IsStateCommand(t.Type))
            .Select(t => TypeNameFormatter.Format(t.Type))
            .ToList();

        if (invalidRequests.Count > 0)
        {
            yield return
                $"Architectural Violation: The following requests implement both IQuery and command state markers ({string.Join(", ", invalidRequests)}). Queries must be read-only.";
        }
    }

    private static IEnumerable<string> FindUnregistrableOpenGenericTypes(ScannedType[] types)
    {
        var invalidTypes = types
            .Where(t => t.Type is { IsClass: true, ContainsGenericParameters: true })
            .Where(t => t.Interfaces.Any(i => !IsRegistrableOpenGenericInterface(t.Type, i)))
            .Select(t => TypeNameFormatter.Format(t.Type))
            .ToList();

        if (invalidTypes.Count > 0)
        {
            yield return
                $"The following open generic types cannot be registered, so they would never run: {string.Join(", ", invalidTypes)}. " +
                $"Handlers and post-commit hooks must be closed types, and an open generic validator must have one type parameter " +
                $"that is the request type, as in V<TRequest> : IValidator<TRequest>. Make the type abstract and derive closed types from it.";
        }
    }

    private static IEnumerable<string> FindHooksForPlainCommands(Registration[] hooks)
    {
        var invalidHooks = hooks
            .Where(h => !IsStateCommand(h.Service.GetGenericArguments()[0]))
            .Select(h => TypeNameFormatter.Format(h.Implementation))
            .Distinct()
            .ToList();

        if (invalidHooks.Count > 0)
        {
            yield return
                $"The following post-commit hooks target commands that do not save state ({string.Join(", ", invalidHooks)}). " +
                $"A post-commit hook runs only for an {nameof(IStateCommand)} or an {nameof(ITransactedStateCommand)}.";
        }
    }

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
            && !HookDefinitions.Contains(definition);
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

    private static bool IsQueryHandlerInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);

    private static bool IsStateCommand(Type type)
        => typeof(IStateCommand).IsAssignableFrom(type) || typeof(ITransactedStateCommand).IsAssignableFrom(type);

    private static bool IsQueryRequest(Type[] interfaces)
        => interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
}
