using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Testing.Core;

public abstract class QueryHandlerTestBase<TQueryHandler, TResult> : TestBase
    where TQueryHandler : class
{
    protected abstract string QueryName { get; }

    protected abstract Task<Result<TResult>> Act();

    protected Mock<ILogger<TQueryHandler>> LoggerMock { get; private set; } = null!;

    protected TQueryHandler QueryHandler { get; set; } = null!;

    [SetUp]
    public void BaseSetUp()
        => LoggerMock = new Mock<ILogger<TQueryHandler>>();

    protected void SetupLogger_FailedValidation(string message)
        => LoggerMock.Setup(LogLevel.Error, $"Query '{QueryName}' failed validation: {message}");
}
