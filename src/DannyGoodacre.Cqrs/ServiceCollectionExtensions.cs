using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs;

public static class ServiceCollectionExtensions
{
    // (Using standard extension method syntax, which maps perfectly to your C# 13 extension blocks)

    /// <summary>
    /// Scans assemblies, registers ICommandHandler<T> and ICommandHandler<T, R>,
    /// and automatically applies logging, validation, and state decorators.
    /// </summary>
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.ScanAndRegisterValidators(assemblies);

        // 1. Scan and register concrete handlers to their generic interfaces
        services.AddHandlers(IsCommandHandlerInterface, assemblies);

        // 2. Apply Decorators (Applied INNERMOST to OUTERMOST)
        // Because each Decorate call wraps the previous one, the LAST one you register becomes the OUTERMOST layer.

        // Inner-most: State saving (only runs if validation/business logic succeeds)
        services.DecorateOpenGeneric(typeof(ICommandHandler<>), typeof(StateCommandHandlerDecorator<>));
        services.DecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(StateCommandHandlerDecorator<,>));

        // Middle: Validation (runs before state, aborts if invalid)
        services.DecorateOpenGeneric(typeof(ICommandHandler<>), typeof(ValidationCommandHandlerDecorator<>));
        services.DecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));

        // Outer-most: Logging (catches exceptions from ALL inner layers, including DB saves)
        services.DecorateOpenGeneric(typeof(ICommandHandler<>), typeof(LoggingCommandHandlerDecorator<>));
        services.DecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));

        return services;
    }

    /// <summary>
    /// Predicate to identify if an interface is one of our generic CQRS handler contracts.
    /// </summary>
    private static bool IsCommandHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        Type genericTypeDef = type.GetGenericTypeDefinition();

        return genericTypeDef == typeof(ICommandHandler<>) || genericTypeDef == typeof(ICommandHandler<,>);
    }

    /// <summary>
    /// Core scanner that finds concrete classes and registers them against matching interfaces.
    /// </summary>
    private static IServiceCollection AddHandlers(
        this IServiceCollection services,
        Func<Type, bool> interfacePredicate,
        params Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x => x is { IsAbstract: false, IsClass: true });

        foreach (Type handlerType in handlerTypes)
        {
            // Find interfaces like ICommandHandler<CreateUserCommand, Guid>
            var targetInterfaces = handlerType.GetInterfaces().Where(interfacePredicate);

            foreach (Type interfaceType in targetInterfaces)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }

    private static IServiceCollection DecorateOpenGeneric(
        this IServiceCollection services,
        Type openServiceType,
        Type openDecoratorType)
    {
        for (int i = 0; i < services.Count; i++)
        {
            ServiceDescriptor descriptor = services[i];

            if (descriptor.ServiceType.IsGenericType &&
                descriptor.ServiceType.GetGenericTypeDefinition() == openServiceType)
            {
                Type closedServiceType = descriptor.ServiceType;
                Type[] genericArgs = closedServiceType.GetGenericArguments();

                // 1. Pre-compute the exact closed decorator type at STARTUP
                Type closedDecoratorType = openDecoratorType.MakeGenericType(genericArgs);

                // 2. Pre-compile constructor matcher at STARTUP using ActivatorUtilities
                ObjectFactory decoratorFactory = ActivatorUtilities.CreateFactory(
                    closedDecoratorType,
                    argumentTypes: [closedServiceType]);

                // 3. Replace descriptor with a factory using the pre-compiled ActivatorUtilities factory
                services[i] = ServiceDescriptor.Describe(
                    closedServiceType,
                    provider =>
                    {
                        object inner = descriptor.ImplementationInstance
                            ?? (descriptor.ImplementationType != null
                                ? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType)
                                : descriptor.ImplementationFactory!(provider));

                        // Zero-reflection instantiation via pre-compiled expression delegate
                        return decoratorFactory(provider, [inner]);
                    },
                    descriptor.Lifetime);
            }
        }

        return services;
    }

    private static void ScanAndRegisterValidators(this IServiceCollection services, Assembly[] assemblies)
    {
        var validatorTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces(), (impl, iface) => new { Impl = impl, Iface = iface })
            .Where(x => x.Iface.IsGenericType && x.Iface.GetGenericTypeDefinition() == typeof(ICommandValidator<>));

        foreach (var reg in validatorTypes)
        {
            // Register validator as Scoped or Transient
            services.AddScoped(reg.Iface, reg.Impl);
        }
    }
}
