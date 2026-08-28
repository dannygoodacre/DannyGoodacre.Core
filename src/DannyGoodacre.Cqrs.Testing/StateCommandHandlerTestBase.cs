using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class StateCommandHandlerTestBase<TCommandHandler>
    : StateCommandHandlerTestCore<TCommandHandler, NUnit.Framework.Result>
    where TCommandHandler : class;

public abstract class StateCommandHandlerTestBase<TCommandHandler, TResultType>
    : StateCommandHandlerTestCore<TCommandHandler, Result<TResultType>>
    where TCommandHandler : class;

public abstract class StateCommandHandlerTestCore<TCommandHandler, TResult>
    : CommandHandlerTestCore<TCommandHandler, TResult>
    where TCommandHandler : class
    where TResult : NUnit.Framework.Result
{
    internal StateCommandHandlerTestCore() { }

    protected Mock<IStateUnit> StateUnitMock { get; private set; } = null!;

    [SetUp]
    public override void BaseSetUp()
    {
        base.BaseSetUp();

        StateUnitMock = new Mock<IStateUnit>(MockBehavior.Strict);
    }

    protected void SetupStateUnit_SaveChangesAsync(int times = 1)
        => StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(0)
            .Verifiable(Times.Exactly(times));
}
