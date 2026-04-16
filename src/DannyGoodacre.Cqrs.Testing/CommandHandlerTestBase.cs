using DannyGoodacre.Primitives;
using DannyGoodacre.Testing.Core;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class CommandHandlerTestBase<TCommandHandler>
    : CommandHandlerTestCore<TCommandHandler, Result>
    where TCommandHandler : class;

public abstract class CommandHandlerTestBase<TCommandHandler, TResultType>
    : CommandHandlerTestCore<TCommandHandler, Result<TResultType>>
    where TCommandHandler : class;

public abstract class CommandHandlerTestCore<TCommandHandler, TResult>
    : TestBase
    where TCommandHandler : class
    where TResult : Result
{
    internal CommandHandlerTestCore() { }

    protected abstract string CommandName { get; }

    protected CancellationToken TestCancellationToken;

    protected Mock<ILogger<TCommandHandler>> LoggerMock { get; private set; } = null!;

    protected TCommandHandler CommandHandler { get; set; } = null!;

    protected abstract Task<TResult> Act();

    [SetUp]
    public virtual void BaseSetUp()
    {
        TestCancellationToken = CancellationToken.None;

        LoggerMock = new Mock<ILogger<TCommandHandler>>(MockBehavior.Strict);

        LoggerMock
            .Setup(x => x.IsEnabled(
                It.Is<LogLevel>(y => y == LogLevel.Information)))
            .Returns(true);

        LoggerMock
            .Setup(x => x.IsEnabled(
                It.Is<LogLevel>(y => y == LogLevel.Error)))
            .Returns(true);

        LoggerMock
            .Setup(x => x.IsEnabled(
                It.Is<LogLevel>(y => y == LogLevel.Critical)))
            .Returns(true);
    }

    protected void SetupLogger_FailedValidation(string message)
        => LoggerMock.Setup(LogLevel.Error, $"Command '{CommandName}' failed validation: {message}");

    protected void SetupLogger_CanceledBeforeExecution()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' was canceled before execution.");

    protected void SetupLogger_CanceledDuringExecution()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' was canceled during execution.");

    protected void SetupLogger_Failed(Exception exception)
        => LoggerMock.Setup(LogLevel.Critical, $"Command '{CommandName}' failed.", exception: exception);
}
