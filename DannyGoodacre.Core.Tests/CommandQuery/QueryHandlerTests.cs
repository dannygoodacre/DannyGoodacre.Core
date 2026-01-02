using DannyGoodacre.Core.CommandQuery;
using Microsoft.Extensions.Logging;
using Moq;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class QueryHandlerTests : TestBase
{
    public class TestQuery : IQuery;

    private const string TestName = "Test Query Handler";

    private const string TestProperty = "Test Property";

    private const string TestError = "Test Error";

    private const string TestExceptionMessage = "Test Exception Message";

    private readonly CancellationToken _testCancellationToken = CancellationToken.None;

    private readonly TestQuery _testQuery = new();

    private static Action<ValidationState, TestQuery> _validate = (_, _) => {};

    private static Func<TestQuery, CancellationToken, Task<Result<int>>> _internalExecuteAsync = (_, _) => Task.FromResult(new Result<int>());

    private Mock<ILogger<TestQueryHandler>> _loggerMock = null!;

    public class TestQueryHandler(ILogger logger) : QueryHandler<TestQuery, int>(logger)
    {
        protected override string QueryName => TestName;

        protected override void Validate(ValidationState validationState, TestQuery query)
            => _validate(validationState, query);

        protected override Task<Result<int>> InternalExecuteAsync(TestQuery query, CancellationToken cancellationToken)
            => _internalExecuteAsync(query, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestQuery command, CancellationToken cancellationToken) => ExecuteAsync(command, cancellationToken);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<TestQueryHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(LogLevel.Error, $"Query '{TestName}' failed validation: {TestProperty}:{Environment.NewLine}  - {TestError}");

        _validate = (validationState, _)
            => validationState.AddError(TestProperty, TestError);

        var handler = new TestQueryHandler(_loggerMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testQuery, _testCancellationToken);

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        _loggerMock = new Mock<ILogger<TestQueryHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(LogLevel.Information, $"Query '{TestName}' was cancelled before execution.");

        var handler = new TestQueryHandler(_loggerMock.Object);

        await cancellationTokenSource.CancelAsync();

        // Act
        var result = await handler.TestExecuteAsync(_testQuery, cancellationTokenSource.Token);

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledDuring_ShouldReturnCancelled()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<TestQueryHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(LogLevel.Information, $"Query '{TestName}' was cancelled during execution.");

        _internalExecuteAsync = (_, _) => throw new OperationCanceledException();

        var handler = new TestQueryHandler(_loggerMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testQuery, _testCancellationToken);

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        var exception = new ApplicationException(TestExceptionMessage);

        _loggerMock = new Mock<ILogger<TestQueryHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(
            LogLevel.Critical,
            $"Query '{TestName}' failed with exception: {TestExceptionMessage}",
            exception: exception);

        _internalExecuteAsync = (_, _) => throw exception;

        var handler = new TestQueryHandler(_loggerMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testQuery, _testCancellationToken);

        // Assert
        AssertInternalError(result, TestExceptionMessage);
    }
}
