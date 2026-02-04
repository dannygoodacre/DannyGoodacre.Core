namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class QueryHandlerTests : QueryHandlerTestBase<QueryHandlerTests.TestQueryHandler, int>
{
    public sealed class TestQuery : IQuery;

    public sealed class TestQueryHandler(ILogger logger) : QueryHandler<TestQuery, int>(logger)
    {
        protected override string QueryName => TestName;

        protected override void Validate(ValidationState validationState, TestQuery query)
            => _testValidate(validationState, query);

        protected override Task<Result<int>> InternalExecuteAsync(TestQuery query, CancellationToken cancellationToken)
            => _testInternalExecuteAsync(query, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestQuery command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Query Handler";

    private const int TestResultValue = 123;

    private static TestQuery _testQuery = null!;

    private static Action<ValidationState, TestQuery> _testValidate = null!;

    private static Func<TestQuery, CancellationToken, Task<Result<int>>> _testInternalExecuteAsync = null!;

    protected override string QueryName => TestName;

    protected override Task<Result<int>> Act() => QueryHandler.TestExecuteAsync(_testQuery, CancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result<int>.Success(TestResultValue));

        _testQuery = new TestQuery();

        QueryHandler = new TestQueryHandler(LoggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        SetupLogger_FailedValidation($"{testProperty}:{Environment.NewLine}  - {testError}");

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledBefore_ShouldReturnCanceled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        CancellationToken = cancellationTokenSource.Token;

        SetupLogger_CanceledBeforeExecution();

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
        AssertSuccess(result, TestResultValue);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledDuring_ShouldReturnCanceled()
    {
        // Arrange
        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        SetupLogger_CanceledDuringExecution();

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

        _testInternalExecuteAsync = (_, _) => throw exception;

        SetupLogger_Failed(exception);

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }
}
