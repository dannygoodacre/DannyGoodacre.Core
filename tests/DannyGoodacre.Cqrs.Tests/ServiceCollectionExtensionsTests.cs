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

    private readonly ServiceCollection _services = new();

    public ServiceCollectionExtensionsTests()
    {
        _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _services.AddSingleton<IStateUnit>(new Mock<IStateUnit>().Object);

        _services.AddSingleton<ITransactionUnit>(new Mock<ITransactionUnit>().Object);
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

        Assert.Single(provider.GetServices<IValidator<TestStateCommand>>());
        Assert.Single(provider.GetServices<IPostCommitHook<TestStateCommand, string>>());
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
