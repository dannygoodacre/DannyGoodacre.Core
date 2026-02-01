using DannyGoodacre.Core.CommandQuery.Abstractions;

namespace DannyGoodacre.Testing.Core;

public abstract class TransactionCommandHandlerTestBase<TCommandHandler>
    : TransactionCommandHandlerTestCore<TCommandHandler>
    where TCommandHandler : class
{
    protected abstract Task<Result> Act();
}

public abstract class TransactionCommandHandlerTestBase<TCommandHandler, TResult>
    : TransactionCommandHandlerTestCore<TCommandHandler>
    where TCommandHandler : class
{
    protected abstract Task<Result<TResult>> Act();
}

public abstract class TransactionCommandHandlerTestCore<TCommandHandler>
    : CommandHandlerTestCore<TCommandHandler>
    where TCommandHandler : class
{
    internal TransactionCommandHandlerTestCore() {}

    protected virtual int TestActualChanges => -1;

    protected Mock<ITransactionalUnitOfWork> UnitOfWorkMock { get; private set; } = null!;

    private Mock<ITransaction> TransactionMock { get; set; } = null!;

    [SetUp]
    public override void BaseSetUp()
    {
        base.BaseSetUp();

        UnitOfWorkMock = new Mock<ITransactionalUnitOfWork>(MockBehavior.Strict);
        TransactionMock = new Mock<ITransaction>(MockBehavior.Strict);

        SetupUnitOfWork_BeginTransactionAsync();

        SetupTransaction_DisposeAsync();
    }

    protected void SetupTransaction_SaveChangesAndCommitAsync()
    {
        SetupUnitOfWork_SaveChangesAsync();

        SetupTransaction_CommitAsync();
    }

    protected void SetupTransaction_RollbackAsync()
        => TransactionMock
            .Setup(x => x.RollbackAsync(
                It.Is<CancellationToken>(y => y == CancellationToken)))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    private void SetupUnitOfWork_BeginTransactionAsync()
        => UnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(
                It.Is<CancellationToken>(y => y == CancellationToken)))
            .ReturnsAsync(TransactionMock.Object)
            .Verifiable(Times.Once);

    private void SetupTransaction_DisposeAsync()
        => TransactionMock
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable(Times.Once);

    private void SetupTransaction_CommitAsync()
        => TransactionMock
            .Setup(x => x.CommitAsync(
                It.Is<CancellationToken>(y => y == CancellationToken)))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    private void SetupUnitOfWork_SaveChangesAsync()
        => UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == CancellationToken)))
            .ReturnsAsync(TestActualChanges)
            .Verifiable(Times.Once);
}
