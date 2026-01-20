using DannyGoodacre.Core.CommandQuery.Abstractions;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Testing.Core;

public abstract class TransactionCommandHandlerTestBase<TTransactionCommandHandler>
    : CommandHandlerTestBase<TTransactionCommandHandler>
    where TTransactionCommandHandler : class
{
    protected virtual int TestActualChanges => -1;

    protected Mock<IUnitOfWork> UnitOfWorkMock { get; private set; } = null!;

    private Mock<ITransaction> TransactionMock { get; set; } = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();

        UnitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

        TransactionMock = new Mock<ITransaction>(MockBehavior.Strict);

        SetupUnitOfWork_BeginTransactionAsync();

        SetupTransaction_DisposeAsync();
    }

    protected void SetupTransaction_SaveAndCommit()
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
