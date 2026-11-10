namespace DannyGoodacre.Cqrs.Tests;

public sealed class QueryLoggingDecoratorTests
{
    public sealed class TestQuery : IQuery<string>;

    private sealed class QueryLoggingTestContext<TQuery, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
        : TestContext<TQuery>
        where TQuery : class, IQuery<TResult>
        where TResult : notnull
    {
        public Mock<IQueryHandler<TQuery, TResult>> InnerMock { get; } = new(mockBehavior);

        public IQueryHandler<TQuery, TResult> CreatePipeline()
            => new QueryLoggingDecorator<TQuery, TResult>(InnerMock.Object, Logger);
    }

    private readonly QueryLoggingTestContext<TestQuery, string> _context = new();

    [Fact]
    public async Task HandleAsync_DelegatesToInnerHandlerAndExecutesLoggingPipeline()
    {
        // Arrange
        var query = new TestQuery();

        const string expectedResponse = "Response";

        var expectedResult = new Success<string>(expectedResponse);

        _context.InnerMock
            .Setup(x => x.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var pipeline = _context.CreatePipeline();

        // Act
        IResult<string> result = await pipeline.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedResponse);

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);
        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestSucceeded, LogLevel.Information);
    }
}
