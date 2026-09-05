using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class QueryHandlerTestBase<TQueryHandler, TResult> : TestBase
    where TQueryHandler : class
{
    protected abstract string QueryName { get; }

    protected CancellationToken TestCancellationToken;

    protected Mock<ILogger<TQueryHandler>> LoggerMock { get; private set; } = null!;

    protected TQueryHandler QueryHandler { get; set; } = null!;

    protected abstract Task<IResult<TResult>> Act();

    [SetUp]
    public void BaseSetUp()
    {
        TestCancellationToken = CancellationToken.None;

        LoggerMock = new Mock<ILogger<TQueryHandler>>(MockBehavior.Strict);
    }
}
