using DannyGoodacre.Primitives;
using Moq;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Test context for building and testing void-returning transacted command handler pipelines.
/// </summary>
public class TransactedStateCommandTestContext<TCommand>(MockBehavior mockBehavior = MockBehavior.Default)
    : StateCommandTestContextCore<TCommand, ICommandHandler<TCommand>, ITransactionUnit>(mockBehavior)
    where TCommand : class, ICommand
{
    public Mock<IPostCommitHook<TCommand>> PostCommitHookMock { get; } = new(mockBehavior);

    public Mock<ITransactionUnit> TransactionUnitMock => StateUnitMock;

    public void SetupExecuteInTransaction()
    {
        TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>(
                (operation, ct) => operation(ct));
    }

    public void SetupPostCommitHook(TCommand command, IResult expectedResult)
    {
        PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

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
public class TransactedStateCommandTestContext<TCommand, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : StateCommandTestContextCore<TCommand, ICommandHandler<TCommand, TResult>, ITransactionUnit>(mockBehavior)
    where TCommand : class, ICommand
    where TResult : notnull
{
    public Mock<IPostCommitHook<TCommand, TResult>> PostCommitHookMock { get; } = new(mockBehavior);

    public Mock<ITransactionUnit> TransactionUnitMock => StateUnitMock;

    public void SetupExecuteInTransaction()
    {
        TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult<TResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult<TResult>>>, CancellationToken>(
                (operation, ct) => operation(ct));
    }

    public void SetupPostCommitHook(TCommand command, IResult<TResult> expectedResult)
    {
        PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

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
