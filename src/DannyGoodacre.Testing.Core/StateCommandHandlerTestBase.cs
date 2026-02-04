using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Testing.Core;

public abstract class StateCommandHandlerTestBase<TCommandHandler>
    : StateCommandHandlerTestCore<TCommandHandler, Result>
    where TCommandHandler : class;

public abstract class StateCommandHandlerTestBase<TCommandHandler, TResultType>
    : StateCommandHandlerTestCore<TCommandHandler, Result<TResultType>>
    where TCommandHandler : class;

public abstract class StateCommandHandlerTestCore<TCommandHandler, TResult>
    : CommandHandlerTestCore<TCommandHandler, TResult>
    where TCommandHandler : class
    where TResult : Result
{
    internal StateCommandHandlerTestCore() { }

    [SetUp]
    public void StateSetUp()
    {
        StateUnitMock =  new Mock<IStateUnit>(MockBehavior.Strict);
    }

    protected Mock<IStateUnit> StateUnitMock { get; private set; } = null!;

    protected void SetupLogger_CanceledWhilePersistingChanges()
        => LoggerMock.Setup(LogLevel.Information,
            $"Command '{CommandName}' was canceled while persisting changes.");

    protected void SetupLogger_FailedWhilePersistingChanges(Exception exception)
        => LoggerMock.Setup(LogLevel.Critical,
            $"Command '{CommandName}' failed while persisting changes.", exception: exception);

    protected void SetupStateUnit_SaveChangesAsync()
        => StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(0)
            .Verifiable(Times.Once);
}
