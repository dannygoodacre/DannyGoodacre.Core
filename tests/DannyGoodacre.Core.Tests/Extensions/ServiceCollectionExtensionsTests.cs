using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Core.Tests.Extensions;

[TestFixture]
public sealed class ServiceCollectionExtensionsTests
{
    private interface ITestCommand;

    private sealed class TestCommandHandler(ILogger logger)
        : CommandHandler<ICommand>(logger), ITestCommand
    {
        protected override string CommandName => "Test Command";

        protected override Task<Result> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }

    private interface ITestCommandWithReturnValue;

    private sealed class TestCommandWithReturnValueHandler(ILogger logger)
        : CommandHandler<ICommand, int>(logger), ITestCommandWithReturnValue
    {
        protected override string CommandName => "Test Command With Return Value";

        protected override Task<Result<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success(123));
    }

    private sealed class StateUnit : IStateUnit
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }

    private interface IStateCommand;

    private sealed class TestStateCommandHandler(ILogger logger)
        : StateCommandHandler<ICommand>(logger, new StateUnit()), IStateCommand
    {

        protected override string CommandName => "Test State Command";

        protected override Task<Result> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }

    private interface IStateCommandWithReturnValue;

    private sealed class TestStateCommandHandlerWithReturnValue(ILogger logger)
        : StateCommandHandler<ICommand, int>(logger, new StateUnit()), IStateCommandWithReturnValue
    {

        protected override string CommandName => "Test State Command With Return Value";

        protected override Task<Result<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success(123));
    }

    private sealed class TestTransactionUnit : ITransactionUnit
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private interface ITestTransactionCommand;

    private sealed class TestTransactionCommandHandler(ILogger logger)
        : TransactionCommandHandler<ICommand>(logger, new TestTransactionUnit()), ITestTransactionCommand
    {
        protected override string CommandName => "Test Transaction Command";

        protected override Task<Result> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }

    private interface ITestTransactionCommandWithReturnValue;

    private sealed class TestTransactionCommandWithReturnValue(ILogger logger)
        : TransactionCommandHandler<ICommand, int>(logger, new TestTransactionUnit()), ITestTransactionCommandWithReturnValue
    {
        protected override string CommandName => "Test Transaction Command With Return Value";

        protected override Task<Result<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success(123));
    }

    private interface ITestQuery;

    private sealed class TestQueryHandler(ILogger logger) : QueryHandler<IQuery, int>(logger), ITestQuery
    {
        protected override string QueryName => "Test Query";

        protected override Task<Result<int>> InternalExecuteAsync(IQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success(123));
    }

    [Test]
    public void AddCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Mock.Of<ILogger>());

        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCommandHandlers(assembly);

        // Assert
        var provider = services.BuildServiceProvider();

        var command = provider.GetService<ITestCommand>();

        Assert.That(command, Is.Not.Null);

        var commandWithValue = provider.GetService<ITestCommand>();

        Assert.That(commandWithValue, Is.Not.Null);

        var testUnitOfWorkCommand = provider.GetService<ITestTransactionCommand>();

        Assert.That(testUnitOfWorkCommand, Is.Not.Null);

        var testUnitOfWorkCommandWithValue = provider.GetService<ITestTransactionCommandWithReturnValue>();

        Assert.That(testUnitOfWorkCommandWithValue, Is.Not.Null);

        using var scope = provider.CreateScope();

        var commandHandler1 = scope.ServiceProvider.GetRequiredService<ITestCommand>();
        var commandHandler2 = scope.ServiceProvider.GetRequiredService<ITestCommand>();

        Assert.That(commandHandler1, Is.SameAs(commandHandler2));

        var commandHandlerWithReturnValue1 = scope.ServiceProvider.GetRequiredService<ITestCommandWithReturnValue>();
        var commandHandlerWithReturnValue2 = scope.ServiceProvider.GetRequiredService<ITestCommandWithReturnValue>();

        Assert.That(commandHandlerWithReturnValue1, Is.SameAs(commandHandlerWithReturnValue2));

        var stateCommandHandler1 = scope.ServiceProvider.GetRequiredService<IStateCommand>();
        var stateCommandHandler2 = scope.ServiceProvider.GetRequiredService<IStateCommand>();

        Assert.That(stateCommandHandler1, Is.SameAs(stateCommandHandler2));

        var stateCommandHandlerWithReturnValue1 = scope.ServiceProvider.GetRequiredService<IStateCommandWithReturnValue>();
        var stateCommandHandlerWithReturnValue2 = scope.ServiceProvider.GetRequiredService<IStateCommandWithReturnValue>();

        Assert.That(stateCommandHandlerWithReturnValue1, Is.SameAs(stateCommandHandlerWithReturnValue2));

        var transactionCommandHandler1 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommand>();
        var transactionCommandHandler2 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommand>();

        Assert.That(transactionCommandHandler1, Is.SameAs(transactionCommandHandler2));

        var transactionCommandHandlerWithReturnValue1 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommandWithReturnValue>();
        var transactionCommandHandlerWithReturnValue2 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommandWithReturnValue>();

        Assert.That(transactionCommandHandlerWithReturnValue1, Is.SameAs(transactionCommandHandlerWithReturnValue2));
    }

    [Test]
    public void AddQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Mock.Of<ILogger>());

        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddQueryHandlers(assembly);

        // Assert
        var provider = services.BuildServiceProvider();

        var registeredHandler = provider.GetService<ITestQuery>();

        Assert.That(registeredHandler, Is.Not.Null);

        using var scope = provider.CreateScope();

        var query1 = scope.ServiceProvider.GetRequiredService<ITestQuery>();
        var query2 = scope.ServiceProvider.GetRequiredService<ITestQuery>();

        Assert.That(query1, Is.SameAs(query2));
    }
}
