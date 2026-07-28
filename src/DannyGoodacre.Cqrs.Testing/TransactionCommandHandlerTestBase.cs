using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class TransactionCommandHandlerTestBase<TCommandHandler>
    : TransactionCommandHandlerTestCore<TCommandHandler, Result>
    where TCommandHandler : class;

public abstract class TransactionCommandHandlerTestBase<TCommandHandler, TResultType>
    : TransactionCommandHandlerTestCore<TCommandHandler, Result<TResultType>>
    where TCommandHandler : class;

public abstract class TransactionCommandHandlerTestCore<TCommandHandler, TResult>
    : CommandHandlerTestCore<TCommandHandler, TResult>
    where TCommandHandler : class
    where TResult : Result
{
    internal TransactionCommandHandlerTestCore() { }

    protected virtual int TestActualChanges => -1;

    protected Mock<ITransactionUnit> TransactionUnitMock { get; private set; } = null!;

    private Mock<ITransaction> TransactionMock { get; set; } = null!;

    [SetUp]
    public override void BaseSetUp()
    {
        base.BaseSetUp();

        TransactionUnitMock = new Mock<ITransactionUnit>(MockBehavior.Strict);

        TransactionMock = new Mock<ITransaction>(MockBehavior.Strict);

        // SetupTransactionUnit_BeginTransactionAsync();

        // SetupTransaction_DisposeAsync();
    }

    protected void SetupLogger_UnexpectedNumberOfChanges(int expected, int actual)
        => LoggerMock
            .Setup(LogLevel.Error, $"Command '{CommandName}' attempted to persist an unexpected number of changes: Expected '{expected}', Actual '{actual}'.");

    protected void Setup_SaveChangesAndCommitAsync()
    {
        SetupTransactionUnit_SaveChangesAsync();

        SetupTransaction_CommitAsync();
    }

    protected void SetupTransaction_CommitAsync()
        => TransactionMock
            .Setup(x => x.CommitAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    protected void SetupTransaction_RollbackAsync()
        => TransactionMock
            .Setup(x => x.RollbackAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    protected void SetupTransactionUnit_SaveChangesAsync()
        => TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(TestActualChanges)
            .Verifiable(Times.Once);

    private void SetupTransaction_DisposeAsync()
        => TransactionMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable(Times.Once);

    protected void SetupTransactionUnit_ExecuteInTransactionAsync()
        => TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Result>>, CancellationToken>(
                (operation, ct) => operation(ct))
            .Verifiable(Times.Once);
}
