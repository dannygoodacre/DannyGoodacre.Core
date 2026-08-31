using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class TransactionCommandHandlerTestBase<TCommandHandler>
    : TransactionCommandHandlerTestCore<TCommandHandler, IResult>
    where TCommandHandler : class;

public abstract class TransactionCommandHandlerTestBase<TCommandHandler, TResultType>
    : TransactionCommandHandlerTestCore<TCommandHandler, IResult<TResultType>>
    where TCommandHandler : class;

public abstract class TransactionCommandHandlerTestCore<TCommandHandler, TResult>
    : CommandHandlerTestCore<TCommandHandler, IResult>
    where TCommandHandler : class
    where TResult : IResult
{
    internal TransactionCommandHandlerTestCore() { }

    protected virtual int TestActualChanges => -1;

    protected Mock<ITransactionUnit> TransactionUnitMock { get; private set; } = null!;

    [SetUp]
    public override void BaseSetUp()
    {
        base.BaseSetUp();

        TransactionUnitMock = new Mock<ITransactionUnit>(MockBehavior.Strict);
    }

    protected void SetupTransactionUnit_SaveChangesAsync(int times = 1)
        => TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(TestActualChanges)
            .Verifiable(Times.Exactly(times));

    protected void SetupTransactionUnit_ExecuteInTransactionAsync()
        => TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TResult>>>(),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .Returns<Func<CancellationToken, Task<TResult>>, CancellationToken>(
                (operation, ct) => operation(ct))
            .Verifiable(Times.Once);
}
