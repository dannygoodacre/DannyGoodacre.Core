using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DannyGoodacre.Cqrs.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    public sealed class TestStateCommand : IStateCommand;

    public sealed class TestQuery : IQuery<string>;

    public sealed class PlainCommand : ICommand;

    public sealed class TestCommandHandler : ICommandHandler<TestStateCommand, string>
    {
        public Task<IResult<string>> HandleAsync(TestStateCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<string>>(new Success<string>("Handled"));
    }

    public sealed class PlainCommandHandler : ICommandHandler<PlainCommand>
    {
        public Task<IResult> HandleAsync(PlainCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    public sealed class DisposalProbe
    {
        public bool HandlerDisposed { get; set; }
    }

    public sealed class DisposableCommand : ICommand;

    public sealed class DisposableCommandHandler(DisposalProbe probe) : ICommandHandler<DisposableCommand>, IDisposable
    {
        public Task<IResult> HandleAsync(DisposableCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());

        public void Dispose() => probe.HandlerDisposed = true;
    }

    public sealed class AsyncDisposableCommand : IStateCommand;

    public sealed class AsyncDisposableCommandHandler(DisposalProbe probe) : ICommandHandler<AsyncDisposableCommand, string>, IAsyncDisposable
    {
        public Task<IResult<string>> HandleAsync(AsyncDisposableCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<string>>(new Success<string>("Handled"));

        public ValueTask DisposeAsync()
        {
            probe.HandlerDisposed = true;

            return ValueTask.CompletedTask;
        }
    }

    public interface IUnregisteredDependency;

    public sealed class MissingDependencyCommand : ICommand;

    public sealed class MissingDependencyCommandHandler(IUnregisteredDependency dependency) : ICommandHandler<MissingDependencyCommand>
    {
        public IUnregisteredDependency Dependency { get; } = dependency;

        public Task<IResult> HandleAsync(MissingDependencyCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
    {
        public Task<IResult<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<string>>(new Success<string>("Queried"));
    }

    public sealed class TestValidator : IValidator<TestStateCommand>
    {
        public void Validate(TestStateCommand request, ValidationState state) { }
    }

    public sealed class TestPostCommitHook : IPostCommitHook<TestStateCommand, string>
    {
        public Task AfterCommitAsync(TestStateCommand command, IResult<string> result, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public interface IGroupedRequest;

    public sealed class GroupedCommand : ICommand, IGroupedRequest;

    public sealed class GroupedCommandHandler : ICommandHandler<GroupedCommand>
    {
        public Task<IResult> HandleAsync(GroupedCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    public sealed class GroupedQuery : IQuery<string>, IGroupedRequest;

    public sealed class GroupedQueryHandler : IQueryHandler<GroupedQuery, string>
    {
        public Task<IResult<string>> HandleAsync(GroupedQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<string>>(new Success<string>("Queried"));
    }

    public sealed class GroupedRequestValidator<TRequest> : IValidator<TRequest>
        where TRequest : IRequest, IGroupedRequest
    {
        public void Validate(TRequest request, ValidationState validationState)
            => validationState.AddError("Group", "Rejected by the group validator.");
    }

    public sealed class GenericCommand<T> : ICommand;

    public abstract class GenericCommandHandler<T> : ICommandHandler<GenericCommand<T>>
    {
        public Task<IResult> HandleAsync(GenericCommand<T> command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    public sealed class StringGenericCommandHandler : GenericCommandHandler<string>;

    public interface IAuditedRequest;

    public sealed class AuditedCommand : ICommand, IAuditedRequest;

    // Deliberately not sealed, so a test can derive a closed validator from this open generic one.
    public class AuditedRequestValidator<TRequest> : IValidator<TRequest>
        where TRequest : IRequest, IAuditedRequest
    {
        public void Validate(TRequest request, ValidationState validationState) { }
    }

    private readonly ServiceCollection _services = new();

    public ServiceCollectionExtensionsTests()
    {
        _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _services.AddSingleton(new Mock<IStateUnit>().Object);

        _services.AddSingleton(new Mock<ITransactionUnit>().Object);
    }

    [Fact]
    public void AddCqrsHandlers_WhenQueryImplementsStateCommandMarker_ThrowsInvalidOperationException()
    {
        // Arrange
        var assemblyName = new AssemblyName("DynamicViolationAssembly");

        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType("BadQuery", TypeAttributes.Public | TypeAttributes.Class);

        typeBuilder.AddInterfaceImplementation(typeof(IQuery<string>));

        typeBuilder.AddInterfaceImplementation(typeof(IStateCommand));

        typeBuilder.CreateType();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _services.AddCqrsHandlers(assemblyBuilder));

        Assert.Contains("Architectural Violation", exception.Message);

        Assert.Contains("BadQuery", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_RegistersValidatorsAndPostCommitHooksInServiceCollection()
    {
        // Arrange
        Assembly testAssembly = typeof(TestCommandHandler).Assembly;

        // Act
        _services.AddCqrsHandlers(testAssembly);

        using var provider = _services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IValidator<TestStateCommand>>();

        Assert.NotNull(validator);
        Assert.IsType<TestValidator>(validator);

        var hook = provider.GetService<IPostCommitHook<TestStateCommand, string>>();

        Assert.NotNull(hook);
        Assert.IsType<TestPostCommitHook>(hook);
    }

    [Fact]
    public void AddCqrsHandlers_DecoratesCommandHandlerInCorrectPipelineOrder()
    {
        // Arrange
        Assembly testAssembly = typeof(TestCommandHandler).Assembly;

        _services.AddCqrsHandlers(testAssembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var handler = provider.GetRequiredService<ICommandHandler<TestStateCommand, string>>();

        // Assert
        var loggingDecorator = Assert.IsType<CommandLoggingDecorator<TestStateCommand, string>>(handler);

        object validationDecoratorObj = GetPrivateFieldValue(loggingDecorator, "inner");
        var validationDecorator = Assert.IsType<CommandValidationDecorator<TestStateCommand, string>>(validationDecoratorObj);

        object transactedDecoratorObj = GetPrivateFieldValue(validationDecorator, "inner");
        var transactedDecorator = Assert.IsType<TransactedStateCommandDecorator<TestStateCommand, string>>(transactedDecoratorObj);

        object stateDecoratorObj = GetPrivateFieldValue(transactedDecorator, "inner");
        var stateDecorator = Assert.IsType<StateCommandDecorator<TestStateCommand, string>>(stateDecoratorObj);

        object coreHandlerObj = GetPrivateFieldValue(stateDecorator, "inner");
        Assert.IsType<TestCommandHandler>(coreHandlerObj);
    }

    [Fact]
    public void AddCqrsHandlers_DecoratesQueryHandlerInCorrectPipelineOrder()
    {
        // Arrange
        Assembly testAssembly = typeof(TestQueryHandler).Assembly;

        _services.AddCqrsHandlers(testAssembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, string>>();

        // Assert
        var loggingDecorator = Assert.IsType<QueryLoggingDecorator<TestQuery, string>>(handler);

        object validationDecoratorObj = GetPrivateFieldValue(loggingDecorator, "inner");
        var validationDecorator = Assert.IsType<QueryValidationDecorator<TestQuery, string>>(validationDecoratorObj);

        object coreHandlerObj = GetPrivateFieldValue(validationDecorator, "inner");
        Assert.IsType<TestQueryHandler>(coreHandlerObj);
    }

    [Fact]
    public void AddCqrsHandlers_WhenQueryStructImplementsStateCommandMarker_ThrowsInvalidOperationException()
    {
        // Arrange
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicStructAssembly"), AssemblyBuilderAccess.Run);

        var typeBuilder = assemblyBuilder
            .DefineDynamicModule("MainModule")
            .DefineType("BadStructQuery", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));

        typeBuilder.AddInterfaceImplementation(typeof(IQuery<string>));

        typeBuilder.AddInterfaceImplementation(typeof(IStateCommand));

        typeBuilder.CreateType();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assemblyBuilder));

        Assert.Contains("BadStructQuery", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHookTargetsPlainCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssemblyWithHook("PlainCommandHook", typeof(IPostCommitHook<PlainCommand>));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("PlainCommandHook", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHookHasNoResultTypeButHandlerReturnsValue_ThrowsInvalidOperationException()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(TestCommandHandler).Assembly);

        // The handler is ICommandHandler<TestStateCommand, string>, so this hook would never run.
        AssemblyBuilder assembly = CreateAssemblyWithHook("UntypedHook", typeof(IPostCommitHook<TestStateCommand>));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("UntypedHook", exception.Message);
        Assert.Contains("ICommandHandler<TestStateCommand>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHookResultTypeDiffersFromHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(TestCommandHandler).Assembly);

        AssemblyBuilder assembly = CreateAssemblyWithHook("WrongResultHook", typeof(IPostCommitHook<TestStateCommand, int>));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("WrongResultHook", exception.Message);
        Assert.Contains("ICommandHandler<TestStateCommand, Int32>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHookHandlerIsRegisteredInEarlierCall_RegistersHook()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(TestCommandHandler).Assembly);

        AssemblyBuilder assembly = CreateAssemblyWithHook("ModuleHook", typeof(IPostCommitHook<TestStateCommand, string>));

        // Act
        _services.AddCqrsHandlers(assembly);

        // Assert
        Assert.Contains(_services, d => d.ServiceType == typeof(IPostCommitHook<TestStateCommand, string>) && d.ImplementationType?.Name == "ModuleHook");
    }

    [Fact]
    public void AddCqrsHandlers_WhenCommandIsPlain_ResolvesWithoutPersistenceUnits()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddCqrsHandlers(typeof(PlainCommandHandler).Assembly);

        using var provider = services.BuildServiceProvider();

        // Act
        var handler = provider.GetRequiredService<ICommandHandler<PlainCommand>>();

        // Assert
        var loggingDecorator = Assert.IsType<CommandLoggingDecorator<PlainCommand>>(handler);

        object validationDecoratorObj = GetPrivateFieldValue(loggingDecorator, "inner");
        var validationDecorator = Assert.IsType<CommandValidationDecorator<PlainCommand>>(validationDecoratorObj);

        Assert.IsType<PlainCommandHandler>(GetPrivateFieldValue(validationDecorator, "inner"));
    }

    [Fact]
    public void AddCqrsHandlers_WhenScopeEnds_DisposesDisposableHandler()
    {
        // Arrange
        var probe = new DisposalProbe();

        _services.AddSingleton(probe);
        _services.AddCqrsHandlers(typeof(DisposableCommandHandler).Assembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ICommandHandler<DisposableCommand>>();
        }

        // Assert
        Assert.True(probe.HandlerDisposed);
    }

    [Fact]
    public async Task AddCqrsHandlers_WhenAsyncScopeEnds_DisposesAsyncDisposableStateCommandHandler()
    {
        // Arrange
        var probe = new DisposalProbe();

        _services.AddSingleton(probe);
        _services.AddCqrsHandlers(typeof(AsyncDisposableCommandHandler).Assembly);

        await using var provider = _services.BuildServiceProvider();

        // Act
        await using (var scope = provider.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICommandHandler<AsyncDisposableCommand, string>>();
        }

        // Assert
        Assert.True(probe.HandlerDisposed);
    }

    [Fact]
    public void AddCqrsHandlers_WhenValidatingOnBuild_ReportsMissingHandlerDependency()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(MissingDependencyCommandHandler).Assembly);

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() =>
            _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

        Assert.Contains(nameof(IUnregisteredDependency), exception.Message);
        Assert.Contains(nameof(MissingDependencyCommandHandler), exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_ResolvesOnlyTheDecoratedHandlerThroughTheInterface()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(PlainCommandHandler).Assembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var handlers = provider.GetServices<ICommandHandler<PlainCommand>>().ToList();

        // Assert
        var handler = Assert.Single(handlers);

        Assert.IsType<CommandLoggingDecorator<PlainCommand>>(handler);
    }

    [Fact]
    public void AddCqrsHandlers_WhenValidatorIsOpenGeneric_RegistersItForRequestsMeetingItsConstraints()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(GroupedRequestValidator<>).Assembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var groupedValidators = provider.GetServices<IValidator<GroupedCommand>>().ToList();
        var otherValidators = provider.GetServices<IValidator<TestStateCommand>>().ToList();

        // Assert
        Assert.IsType<GroupedRequestValidator<GroupedCommand>>(Assert.Single(groupedValidators));
        Assert.IsType<TestValidator>(Assert.Single(otherValidators));
    }

    [Fact]
    public async Task AddCqrsHandlers_WhenOpenGenericValidatorRejectsCommand_ReturnsInvalid()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(GroupedCommandHandler).Assembly);

        using var provider = _services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<GroupedCommand>>();

        // Act
        IResult result = await handler.HandleAsync(new GroupedCommand(), TestContext.Current.CancellationToken);

        // Assert
        var invalid = Assert.IsType<Invalid>(result);
        Assert.True(invalid.ValidationState.HasErrors);
    }

    [Fact]
    public async Task AddCqrsHandlers_WhenOpenGenericValidatorRejectsQuery_ReturnsInvalid()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(GroupedQueryHandler).Assembly);

        using var provider = _services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<GroupedQuery, string>>();

        // Act
        IResult<string> result = await handler.HandleAsync(new GroupedQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<Invalid<string>>(result);
    }

    [Fact]
    public void AddCqrsHandlers_WhenCalledTwice_RegistersOpenGenericValidatorOneTime()
    {
        // Arrange
        Assembly testAssembly = typeof(GroupedRequestValidator<>).Assembly;

        _services.AddCqrsHandlers(testAssembly);
        _services.AddCqrsHandlers(testAssembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var validators = provider.GetServices<IValidator<GroupedCommand>>();

        // Assert
        Assert.Single(validators);
    }

    [Fact]
    public void AddCqrsHandlers_WhenOpenGenericValidatorDoesNotValidateItsTypeParameter_ThrowsInvalidOperationException()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssemblyWithOpenGenericType(
            "WrappedValidator", typeof(IValidator<>), t => [typeof(GenericCommand<>).MakeGenericType(t)]);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("WrappedValidator<T>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenOpenGenericValidatorValidatesClosedRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssemblyWithOpenGenericType(
            "UnusedParameterValidator", typeof(IValidator<>), _ => [typeof(TestStateCommand)]);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("UnusedParameterValidator<T>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenCommandHandlerIsOpenGeneric_ThrowsInvalidOperationException()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssemblyWithOpenGenericType(
            "OpenCommandHandler", typeof(ICommandHandler<,>), t => [t, typeof(string)], constraint: typeof(ICommand));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("OpenCommandHandler<T>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenQueryHandlerIsOpenGeneric_ThrowsInvalidOperationException()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssemblyWithOpenGenericType(
            "OpenQueryHandler", typeof(IQueryHandler<,>), t => [t, typeof(string)], constraint: typeof(IQuery<string>));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("OpenQueryHandler<T>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHookIsOpenGeneric_ThrowsInvalidOperationException()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssemblyWithOpenGenericType(
            "OpenHook", typeof(IPostCommitHook<>), t => [t], constraint: typeof(IStateCommand));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("OpenHook<T>", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHandlerDerivesFromAbstractOpenGenericBase_DecoratesTheClosedHandler()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(StringGenericCommandHandler).Assembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var handler = provider.GetRequiredService<ICommandHandler<GenericCommand<string>>>();

        // Assert
        var loggingDecorator = Assert.IsType<CommandLoggingDecorator<GenericCommand<string>>>(handler);

        object validationDecoratorObj = GetPrivateFieldValue(loggingDecorator, "inner");
        var validationDecorator = Assert.IsType<CommandValidationDecorator<GenericCommand<string>>>(validationDecoratorObj);

        Assert.IsType<StringGenericCommandHandler>(GetPrivateFieldValue(validationDecorator, "inner"));
    }

    // Emits a non-abstract open generic type T`1 that implements openInterface closed over interfaceArguments(T).
    // Each method returns null or nothing; the tests only scan the type, so its methods never run.
    private static AssemblyBuilder CreateAssemblyWithOpenGenericType(
        string typeName,
        Type openInterface,
        Func<Type, Type[]> interfaceArguments,
        Type? constraint = null)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Dynamic{typeName}Assembly"), AssemblyBuilderAccess.Run);

        var typeBuilder = assemblyBuilder
            .DefineDynamicModule("MainModule")
            .DefineType($"{typeName}`1", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        GenericTypeParameterBuilder typeParameter = typeBuilder.DefineGenericParameters("T")[0];

        if (constraint is not null)
        {
            typeParameter.SetInterfaceConstraints(constraint);
        }

        Type[] arguments = interfaceArguments(typeParameter);

        Type interfaceType = openInterface.MakeGenericType(arguments);

        bool interfaceUsesTypeParameter = interfaceType.GetType() != typeof(object).GetType();

        typeBuilder.AddInterfaceImplementation(interfaceType);

        foreach (MethodInfo openMethod in openInterface.GetMethods())
        {
            MethodBuilder method = typeBuilder.DefineMethod(
                openMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                Substitute(openMethod.ReturnType, arguments),
                [.. openMethod.GetParameters().Select(p => Substitute(p.ParameterType, arguments))]);

            ILGenerator il = method.GetILGenerator();

            if (openMethod.ReturnType != typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(
                method,
                interfaceUsesTypeParameter
                    ? TypeBuilder.GetMethod(interfaceType, openMethod)
                    : interfaceType.GetMethod(openMethod.Name)!);
        }

        typeBuilder.CreateType();

        return assemblyBuilder;

        static Type Substitute(Type type, Type[] arguments)
            => type.IsGenericParameter ? arguments[type.GenericParameterPosition]
                : type.IsGenericType ? type.GetGenericTypeDefinition().MakeGenericType([.. type.GetGenericArguments().Select(a => Substitute(a, arguments))])
                : type;
    }

    private static AssemblyBuilder CreateAssemblyWithHook(string typeName, Type hookInterface)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Dynamic{typeName}Assembly"), AssemblyBuilderAccess.Run);

        MethodInfo interfaceMethod = hookInterface.GetMethod("AfterCommitAsync")!;

        var typeBuilder = assemblyBuilder
            .DefineDynamicModule("MainModule")
            .DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        typeBuilder.AddInterfaceImplementation(hookInterface);

        MethodBuilder method = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            typeof(Task),
            [.. interfaceMethod.GetParameters().Select(p => p.ParameterType)]);

        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(method, interfaceMethod);

        typeBuilder.CreateType();

        return assemblyBuilder;
    }

    [Fact]
    public void AddCqrsHandlers_WhenCalledTwice_DecoratesEachHandlerOneTime()
    {
        // Arrange
        Assembly testAssembly = typeof(TestCommandHandler).Assembly;

        _services.AddCqrsHandlers(testAssembly);
        _services.AddCqrsHandlers(testAssembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var handler = provider.GetRequiredService<ICommandHandler<TestStateCommand, string>>();

        // Assert
        var loggingDecorator = Assert.IsType<CommandLoggingDecorator<TestStateCommand, string>>(handler);

        Assert.IsType<CommandValidationDecorator<TestStateCommand, string>>(GetPrivateFieldValue(loggingDecorator, "inner"));

        Assert.Single(provider.GetServices<ICommandHandler<TestStateCommand, string>>());
        Assert.Single(provider.GetServices<IValidator<TestStateCommand>>());
        Assert.Single(provider.GetServices<IPostCommitHook<TestStateCommand, string>>());
    }

    [Fact]
    public void AddCqrsHandlers_WhenAssemblyIsPassedTwiceInOneCall_RegistersEachHandlerOneTime()
    {
        // Arrange
        Assembly testAssembly = typeof(PlainCommandHandler).Assembly;

        _services.AddCqrsHandlers(testAssembly, testAssembly);

        using var provider = _services.BuildServiceProvider();

        // Act
        var handlers = provider.GetServices<ICommandHandler<PlainCommand>>();

        // Assert
        Assert.Single(handlers);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHandlerDerivesFromConcreteHandler_ThrowsNamingBoth()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssembly(
            "ConcreteBaseHandler",
            new EmittedClass("BaseHandler", Implements: typeof(ICommandHandler<PlainCommand>)),
            new EmittedClass("DerivedHandler", BaseName: "BaseHandler"));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("ICommandHandler<PlainCommand> (BaseHandler, DerivedHandler : BaseHandler)", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenTwoUnrelatedHandlersHandleOneRequest_ThrowsNamingBoth()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssembly(
            "UnrelatedHandlers",
            new EmittedClass("FirstQueryHandler", Implements: typeof(IQueryHandler<TestQuery, string>)),
            new EmittedClass("SecondQueryHandler", Implements: typeof(IQueryHandler<TestQuery, string>)));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("IQueryHandler<TestQuery, String> (FirstQueryHandler, SecondQueryHandler)", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenEarlierCallRegisteredAnotherHandlerForTheRequest_ThrowsNamingBoth()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(PlainCommandHandler).Assembly);

        AssemblyBuilder assembly = CreateAssembly(
            "ModuleHandler",
            new EmittedClass("ModulePlainCommandHandler", Implements: typeof(ICommandHandler<PlainCommand>)));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("ICommandHandler<PlainCommand> (PlainCommandHandler, ModulePlainCommandHandler)", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenValidatorDerivesFromConcreteValidator_ThrowsNamingBoth()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssembly(
            "ConcreteBaseValidator",
            new EmittedClass("BaseValidator", Implements: typeof(IValidator<PlainCommand>)),
            new EmittedClass("DerivedValidator", BaseName: "BaseValidator"));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("DerivedValidator : BaseValidator (IValidator<PlainCommand>)", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenValidatorDerivesFromOpenGenericValidatorOfEarlierCall_ThrowsNamingBoth()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(AuditedRequestValidator<>).Assembly);

        AssemblyBuilder assembly = CreateAssembly(
            "ClosedFromOpenValidator",
            new EmittedClass("ClosedAuditedValidator", BaseType: typeof(AuditedRequestValidator<AuditedCommand>)));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains(
            "ClosedAuditedValidator : AuditedRequestValidator<TRequest> (IValidator<AuditedCommand>)",
            exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenHookDerivesFromConcreteHook_ThrowsNamingBoth()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(TestCommandHandler).Assembly);

        AssemblyBuilder assembly = CreateAssembly(
            "ConcreteBaseHook",
            new EmittedClass("BaseHook", Implements: typeof(IPostCommitHook<TestStateCommand, string>)),
            new EmittedClass("DerivedHook", BaseName: "BaseHook"));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.Contains("DerivedHook : BaseHook (IPostCommitHook<TestStateCommand, String>)", exception.Message);
    }

    [Fact]
    public void AddCqrsHandlers_WhenRequestHasUnrelatedValidatorsAndHooks_RegistersEach()
    {
        // Arrange
        _services.AddCqrsHandlers(typeof(TestCommandHandler).Assembly);

        AssemblyBuilder assembly = CreateAssembly(
            "SiblingValidatorsAndHooks",
            new EmittedClass("SiblingValidator", Implements: typeof(IValidator<TestStateCommand>)),
            new EmittedClass("SiblingHook", Implements: typeof(IPostCommitHook<TestStateCommand, string>)));

        // Act
        _services.AddCqrsHandlers(assembly);

        // Assert
        Assert.Equal(2, _services.Count(d => d.ServiceType == typeof(IValidator<TestStateCommand>)));
        Assert.Equal(2, _services.Count(d => d.ServiceType == typeof(IPostCommitHook<TestStateCommand, string>)));
    }

    [Fact]
    public void AddCqrsHandlers_WhenThereAreSeveralProblems_ReportsEachInOneExceptionAndRegistersNothing()
    {
        // Arrange
        AssemblyBuilder assembly = CreateAssembly(
            "SeveralProblems",
            new EmittedClass("FirstPlainCommandHandler", Implements: typeof(ICommandHandler<PlainCommand>)),
            new EmittedClass("SecondPlainCommandHandler", Implements: typeof(ICommandHandler<PlainCommand>)),
            new EmittedClass("PlainCommandHook", Implements: typeof(IPostCommitHook<PlainCommand>)));

        int servicesBefore = _services.Count;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddCqrsHandlers(assembly));

        Assert.StartsWith("AddCqrsHandlers found 2 problems.", exception.Message);
        Assert.Contains("SecondPlainCommandHandler", exception.Message);
        Assert.Contains("PlainCommandHook", exception.Message);
        Assert.Equal(servicesBefore, _services.Count);
    }

    private sealed record EmittedClass(string Name, Type? Implements = null, string? BaseName = null, Type? BaseType = null);

    // Emits public classes into one dynamic assembly, in order, so a class can derive from one emitted before it.
    // A class that implements an interface gets methods that return null or nothing; the tests only scan the types,
    // so the methods never run.
    private static AssemblyBuilder CreateAssembly(string assemblyName, params EmittedClass[] classes)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Dynamic{assemblyName}Assembly"), AssemblyBuilderAccess.Run);

        ModuleBuilder module = assemblyBuilder.DefineDynamicModule("MainModule");

        var emitted = new Dictionary<string, Type>();

        foreach (EmittedClass emittedClass in classes)
        {
            Type? parent = emittedClass.BaseName is { } baseName ? emitted[baseName] : emittedClass.BaseType;

            TypeBuilder typeBuilder = module.DefineType(emittedClass.Name, TypeAttributes.Public | TypeAttributes.Class, parent);

            if (emittedClass.Implements is { } interfaceType)
            {
                typeBuilder.AddInterfaceImplementation(interfaceType);

                foreach (MethodInfo interfaceMethod in interfaceType.GetMethods())
                {
                    MethodBuilder method = typeBuilder.DefineMethod(
                        interfaceMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                        interfaceMethod.ReturnType,
                        [.. interfaceMethod.GetParameters().Select(p => p.ParameterType)]);

                    ILGenerator il = method.GetILGenerator();

                    if (interfaceMethod.ReturnType != typeof(void))
                    {
                        il.Emit(OpCodes.Ldnull);
                    }

                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(method, interfaceMethod);
                }
            }

            emitted[emittedClass.Name] = typeBuilder.CreateType();
        }

        return assemblyBuilder;
    }

    private static object GetPrivateFieldValue(object target, string fieldName)
    {
        Type type = target.GetType();

        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? type.GetField($"<{fieldName}>P", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name.Contains($"<{fieldName}>", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on type '{type.Name}'.");

        return field.GetValue(target)!;
    }
}
