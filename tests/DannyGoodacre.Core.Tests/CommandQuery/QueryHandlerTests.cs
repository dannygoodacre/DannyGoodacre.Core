using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class QueryHandlerTests : TestBase
{
    public class TestQueryRequest : IQueryRequest;

    public class TestQueryHandler(ILogger logger) : QueryHandler<TestQueryRequest, int>(logger)
    {
        protected override string QueryName => TestName;

        protected override void Validate(ValidationState validationState, TestQueryRequest queryRequest)
            => _testValidate(validationState, queryRequest);

        protected override Task<Result<int>> InternalExecuteAsync(TestQueryRequest queryRequest, CancellationToken cancellationToken)
            => _testInternalExecuteAsync(queryRequest, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestQueryRequest command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Query Handler";

    private int _testResultValue;

    private CancellationToken _testCancellationToken;

    private Mock<ILogger<TestQueryHandler>> _loggerMock = null!;

    private static Action<ValidationState, TestQueryRequest> _testValidate = null!;

    private static Func<TestQueryRequest, CancellationToken, Task<Result<int>>> _testInternalExecuteAsync = null!;

    private static TestQueryRequest _testQueryRequest = null!;

    private static TestQueryHandler _testCommandHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _testResultValue = 123;

        _testCancellationToken = CancellationToken.None;

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result<int>.Success(_testResultValue));

        _loggerMock = new Mock<ILogger<TestQueryHandler>>(MockBehavior.Strict);

        _testQueryRequest = new TestQueryRequest();

        _testCommandHandler = new TestQueryHandler(_loggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        _loggerMock.Setup(LogLevel.Error, $"Query '{TestName}' failed validation: {testProperty}:{Environment.NewLine}  - {testError}");

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledBefore_ShouldReturnCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        _testCancellationToken = cancellationTokenSource.Token;

        _loggerMock.Setup(LogLevel.Information, $"Query '{TestName}' was cancelled before execution.");

        await cancellationTokenSource.CancelAsync();

        // Act
        var result = await Act();

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result, _testResultValue);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledDuring_ShouldReturnCancelled()
    {
        // Arrange
        _loggerMock.Setup(LogLevel.Information, $"Query '{TestName}' was cancelled during execution.");

        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        // Act
        var result = await Act();

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        _loggerMock.Setup(LogLevel.Critical, $"Query '{TestName}' failed with exception: {testExceptionMessage}", exception: exception);

        _testInternalExecuteAsync = (_, _) => throw exception;

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    private Task<Result<int>> Act() => _testCommandHandler.TestExecuteAsync(_testQueryRequest, _testCancellationToken);
}
