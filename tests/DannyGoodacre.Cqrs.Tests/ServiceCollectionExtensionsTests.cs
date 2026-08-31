using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class ServiceCollectionExtensionsTests
{
    private interface ITestCommand;

    private sealed class TestCommandHandler(ILogger logger)
        : CommandHandler<ICommand>(logger), ITestCommand
    {
        protected override string CommandName => "Test Command";

        protected override Task<IResult> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    private interface ITestCommandWithReturnValue;

    private sealed class TestCommandWithReturnValueHandler(ILogger logger)
        : CommandHandler<ICommand, int>(logger), ITestCommandWithReturnValue
    {
        protected override string CommandName => "Test Command With Return Value";

        protected override Task<IResult<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<int>>(new Success<int>(123));
    }

    private sealed class StateUnit : IStateUnit
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private interface IStateCommand;

    private sealed class TestStateCommandHandler(ILogger logger)
        : StateCommandHandler<ICommand>(logger, new StateUnit()), IStateCommand
    {

        protected override string CommandName => "Test State Command";

        protected override Task<IResult> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    private interface IStateCommandWithReturnValue;

    private sealed class TestStateCommandHandlerWithReturnValue(ILogger logger)
        : StateCommandHandler<ICommand, int>(logger, new StateUnit()), IStateCommandWithReturnValue
    {

        protected override string CommandName => "Test State Command With Return Value";

        protected override Task<IResult<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<int>>(new Success<int>(123));
    }

    private sealed class TestTransactionUnit : ITransactionUnit
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            return await operation(cancellationToken);
        }
    }

    private interface ITestTransactionCommand;

    private sealed class TestTransactionCommandHandler(ILogger logger)
        : TransactionCommandHandler<ICommand>(logger, new TestTransactionUnit()), ITestTransactionCommand
    {
        protected override string CommandName => "Test Transaction Command";

        protected override Task<IResult> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult>(new Success());
    }

    private interface ITestTransactionCommandWithReturnValue;

    private sealed class TestTransactionCommandWithReturnValue(ILogger logger)
        : TransactionCommandHandler<ICommand, int>(logger, new TestTransactionUnit()), ITestTransactionCommandWithReturnValue
    {
        protected override string CommandName => "Test Transaction Command With Return Value";

        protected override Task<IResult<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<int>>(new Success<int>(123));
    }

    private interface ITestQuery;

    private sealed class TestQueryHandler(ILogger logger) : QueryHandler<IQuery, int>(logger), ITestQuery
    {
        protected override string QueryName => "Test Query";

        protected override Task<IResult<int>> InternalExecuteAsync(IQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IResult<int>>(new Success<int>(123));
    }

    [Test]
    public void AddCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Mock.Of<ILogger>());

        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCommandHandlers(assembly);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();

        ITestCommand? command = provider.GetService<ITestCommand>();

        Assert.That(command, Is.Not.Null);

        ITestCommand? commandWithValue = provider.GetService<ITestCommand>();

        Assert.That(commandWithValue, Is.Not.Null);

        ITestTransactionCommand? testUnitOfWorkCommand = provider.GetService<ITestTransactionCommand>();

        Assert.That(testUnitOfWorkCommand, Is.Not.Null);

        ITestTransactionCommandWithReturnValue? testUnitOfWorkCommandWithValue = provider.GetService<ITestTransactionCommandWithReturnValue>();

        Assert.That(testUnitOfWorkCommandWithValue, Is.Not.Null);

        using IServiceScope scope = provider.CreateScope();

        ITestCommand commandHandler1 = scope.ServiceProvider.GetRequiredService<ITestCommand>();
        ITestCommand commandHandler2 = scope.ServiceProvider.GetRequiredService<ITestCommand>();

        Assert.That(commandHandler1, Is.SameAs(commandHandler2));

        ITestCommandWithReturnValue commandHandlerWithReturnValue1 = scope.ServiceProvider.GetRequiredService<ITestCommandWithReturnValue>();
        ITestCommandWithReturnValue commandHandlerWithReturnValue2 = scope.ServiceProvider.GetRequiredService<ITestCommandWithReturnValue>();

        Assert.That(commandHandlerWithReturnValue1, Is.SameAs(commandHandlerWithReturnValue2));

        IStateCommand stateCommandHandler1 = scope.ServiceProvider.GetRequiredService<IStateCommand>();
        IStateCommand stateCommandHandler2 = scope.ServiceProvider.GetRequiredService<IStateCommand>();

        Assert.That(stateCommandHandler1, Is.SameAs(stateCommandHandler2));

        IStateCommandWithReturnValue stateCommandHandlerWithReturnValue1 = scope.ServiceProvider.GetRequiredService<IStateCommandWithReturnValue>();
        IStateCommandWithReturnValue stateCommandHandlerWithReturnValue2 = scope.ServiceProvider.GetRequiredService<IStateCommandWithReturnValue>();

        Assert.That(stateCommandHandlerWithReturnValue1, Is.SameAs(stateCommandHandlerWithReturnValue2));

        ITestTransactionCommand transactionCommandHandler1 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommand>();
        ITestTransactionCommand transactionCommandHandler2 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommand>();

        Assert.That(transactionCommandHandler1, Is.SameAs(transactionCommandHandler2));

        ITestTransactionCommandWithReturnValue transactionCommandHandlerWithReturnValue1 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommandWithReturnValue>();
        ITestTransactionCommandWithReturnValue transactionCommandHandlerWithReturnValue2 = scope.ServiceProvider.GetRequiredService<ITestTransactionCommandWithReturnValue>();

        Assert.That(transactionCommandHandlerWithReturnValue1, Is.SameAs(transactionCommandHandlerWithReturnValue2));
    }

    [Test]
    public void AddQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Mock.Of<ILogger>());

        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddQueryHandlers(assembly);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();

        ITestQuery? registeredHandler = provider.GetService<ITestQuery>();

        Assert.That(registeredHandler, Is.Not.Null);

        using IServiceScope scope = provider.CreateScope();

        ITestQuery query1 = scope.ServiceProvider.GetRequiredService<ITestQuery>();
        ITestQuery query2 = scope.ServiceProvider.GetRequiredService<ITestQuery>();

        Assert.That(query1, Is.SameAs(query2));
    }
}
