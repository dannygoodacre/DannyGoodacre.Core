using DannyGoodacre.Primitives;
using Moq;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Test context for building and testing void-returning transacted command handler pipelines.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class TransactedStateCommandTestContext<TCommand>(MockBehavior mockBehavior = MockBehavior.Default)
    : StateCommandTestContextCore<TCommand, ICommandHandler<TCommand>, ITransactionUnit>(mockBehavior)
    where TCommand : class, ICommand
{
    /// <summary>
    /// The mock of the post-commit hook that the decorator runs when <see cref="CreatePipeline"/> includes it.
    /// </summary>
    public Mock<IPostCommitHook<TCommand>> PostCommitHookMock { get; } = new(mockBehavior);

    /// <summary>
    /// The mock of the transaction unit. This is the same mock as
    /// <see cref="StateCommandTestContextCore{TCommand, TInnerHandler, TStateUnit}.StateUnitMock"/>.
    /// </summary>
    public Mock<ITransactionUnit> TransactionUnitMock => StateUnitMock;

    /// <summary>
    /// Configures <see cref="ITransactionUnit.ExecuteInTransactionAsync{TResultWrapper}"/> to run the operation it
    /// receives, so the handler and the save run as they would inside a real transaction.
    /// </summary>
    public void SetupExecuteInTransaction()
    {
        TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>(
                (operation, ct) => operation(ct));
    }

    /// <summary>
    /// Configures <see cref="PostCommitHookMock"/> to complete when it is called with this command and result.
    /// </summary>
    /// <param name="command">The command that the hook expects.</param>
    /// <param name="expectedResult">The handler result that the hook expects.</param>
    public void SetupPostCommitHook(TCommand command, IResult expectedResult)
    {
        PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Creates the transaction decorator around <see cref="StateCommandTestContextCore{TCommand, TInnerHandler, TStateUnit}.InnerMock"/>.
    /// </summary>
    /// <param name="includePostCommitHook">Whether the decorator runs <see cref="PostCommitHookMock"/>.</param>
    /// <returns>The decorator under test.</returns>
    public ICommandHandler<TCommand> CreatePipeline(bool includePostCommitHook = false)
    {
        return new TransactedStateCommandDecorator<TCommand>(
            InnerMock.Object,
            TransactionUnitMock.Object,
            Logger,
            PostCommitScope,
            includePostCommitHook ? [PostCommitHookMock.Object] : []);
    }
}

/// <summary>
/// Test context for building and testing value-returning transacted command handler pipelines.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <typeparam name="TResult">The type of value that the handler returns.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class TransactedStateCommandTestContext<TCommand, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : StateCommandTestContextCore<TCommand, ICommandHandler<TCommand, TResult>, ITransactionUnit>(mockBehavior)
    where TCommand : class, ICommand
    where TResult : notnull
{
    /// <summary>
    /// The mock of the post-commit hook that the decorator runs when <see cref="CreatePipeline"/> includes it.
    /// </summary>
    public Mock<IPostCommitHook<TCommand, TResult>> PostCommitHookMock { get; } = new(mockBehavior);

    /// <summary>
    /// The mock of the transaction unit. This is the same mock as
    /// <see cref="StateCommandTestContextCore{TCommand, TInnerHandler, TStateUnit}.StateUnitMock"/>.
    /// </summary>
    public Mock<ITransactionUnit> TransactionUnitMock => StateUnitMock;

    /// <summary>
    /// Configures <see cref="ITransactionUnit.ExecuteInTransactionAsync{TResultWrapper}"/> to run the operation it
    /// receives, so the handler and the save run as they would inside a real transaction.
    /// </summary>
    public void SetupExecuteInTransaction()
    {
        TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult<TResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult<TResult>>>, CancellationToken>(
                (operation, ct) => operation(ct));
    }

    /// <summary>
    /// Configures <see cref="PostCommitHookMock"/> to complete when it is called with this command and result.
    /// </summary>
    /// <param name="command">The command that the hook expects.</param>
    /// <param name="expectedResult">The handler result that the hook expects.</param>
    public void SetupPostCommitHook(TCommand command, IResult<TResult> expectedResult)
    {
        PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Creates the transaction decorator around <see cref="StateCommandTestContextCore{TCommand, TInnerHandler, TStateUnit}.InnerMock"/>.
    /// </summary>
    /// <param name="includePostCommitHook">Whether the decorator runs <see cref="PostCommitHookMock"/>.</param>
    /// <returns>The decorator under test.</returns>
    public ICommandHandler<TCommand, TResult> CreatePipeline(bool includePostCommitHook = false)
    {
        return new TransactedStateCommandDecorator<TCommand, TResult>(
            InnerMock.Object,
            TransactionUnitMock.Object,
            Logger,
            PostCommitScope,
            includePostCommitHook ? [PostCommitHookMock.Object] : []);
    }
}
