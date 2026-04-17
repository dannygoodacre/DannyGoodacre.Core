using DannyGoodacre.Primitives;
using DannyGoodacre.Testing.Core;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class QueryHandlerTestBase<TQueryHandler, TResultType> : TestBase
    where TQueryHandler : class
{
    protected abstract string QueryName { get; }

    protected CancellationToken CancellationToken;

    protected Mock<ILogger<TQueryHandler>> LoggerMock { get; private set; } = null!;

    protected TQueryHandler QueryHandler { get; set; } = null!;

    protected abstract Task<Result<TResultType>> Act();

    [SetUp]
    public void BaseSetUp()
    {
        CancellationToken = CancellationToken.None;

        LoggerMock = new Mock<ILogger<TQueryHandler>>(MockBehavior.Strict);

        LoggerMock
            .Setup(x => x.IsEnabled(
                It.IsAny<LogLevel>()))
            .Returns(true);
    }

    protected void SetupLogger_FailedValidation(string message)
        => LoggerMock.Setup(LogLevel.Error, $"Query '{QueryName}' failed validation: {message}");

    protected void SetupLogger_CanceledBeforeExecution()
        => LoggerMock.Setup(LogLevel.Information, $"Query '{QueryName}' was canceled before execution.");

    protected void SetupLogger_CanceledDuringExecution()
        => LoggerMock.Setup(LogLevel.Information, $"Query '{QueryName}' was canceled during execution.");

    protected void SetupLogger_Failed(Exception exception)
        => LoggerMock.Setup(LogLevel.Critical, $"Query '{QueryName}' failed.", exception: exception);
}
