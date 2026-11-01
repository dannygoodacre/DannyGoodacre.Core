using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DannyGoodacre.Cqrs.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    public sealed class TestStateCommand : IStateCommand;

    public sealed class TestQuery : IQuery<string>;

    public sealed class TestCommandHandler : ICommandHandler<TestStateCommand, string>
    {
        public Task<IResult<string>> HandleAsync(TestStateCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<string>>(Result<string>.Success("Handled"));
    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
    {
        public Task<IResult<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<string>>(Result<string>.Success("Queried"));
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

        object stateDecoratorObj = GetPrivateFieldValue(validationDecorator, "inner");
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
