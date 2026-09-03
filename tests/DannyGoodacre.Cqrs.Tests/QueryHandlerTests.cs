namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public class QueryHandlerTests : QueryHandlerTestBase<QueryHandlerTests.TestQueryHandler, int>
{
    public sealed class TestQuery : IQuery;

    public sealed class TestQueryHandler(ILogger logger) : QueryHandler<TestQuery, int>(logger)
    {
        protected override string QueryName => TestName;

        protected override void Validate(ValidationState validationState, TestQuery query)
            => _testValidate(validationState, query);

        protected override Task<IResult<int>> InternalExecuteAsync(TestQuery query, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(query, cancellationToken);

        public Task<IResult<int>> TestExecuteAsync(TestQuery query, CancellationToken cancellationToken = default)
            => ExecuteAsync(query, cancellationToken);

        public IResult<int> TestCanceled() => Canceled();

        public IResult<int> TestConflict(string message) => Conflict(message);

        public IResult<int> TestDomainError(string message) => DomainError(message);

        public IResult<int> TestInternalError(string error) => InternalError(error);

        public IResult<int> TestInternalError(Exception exception) => InternalError(exception);

        public IResult<int> TestInvalid(ValidationState validationState) => Invalid(validationState);

        public IResult<int> TestNotFound() => NotFound();

        public IResult<int> TestSuccess(int value) => Success(value);
    }

    private const string TestName = "Test Query Handler";

    private const int TestResultValue = 123;

    private static TestQuery _testQuery = null!;

    private static Action<ValidationState, TestQuery> _testValidate = null!;

    private static Func<TestQuery, CancellationToken, Task<IResult<int>>> _testInternalExecuteAsync = null!;

    protected override string QueryName => TestName;

    protected override Task<IResult<int>> Act() => QueryHandler.TestExecuteAsync(_testQuery, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult<IResult<int>>(new Success<int>(TestResultValue));

        _testQuery = new TestQuery();

        QueryHandler = new TestQueryHandler(LoggerMock.Object);
    }

    [Test]
    public async Task WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        var testValidationState = new ValidationState();

        testValidationState.AddError(testProperty, testError);

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        LoggerMock.IsEnabled();

        LoggerMock.LogQueryFailedValidation(QueryName, $"{testProperty}:{Environment.NewLine}  - {testError}");

        // Act
        IResult<int> result = await Act();

        // Assert
        AssertInvalid(result, testValidationState);
    }

    [Test]
    public async Task WhenCanceledBefore_ShouldReturnCanceled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        TestCancellationToken = cancellationTokenSource.Token;

        LoggerMock.IsEnabled();

        LoggerMock.LogQueryCanceledBeforeExecution(QueryName);

        await cancellationTokenSource.CancelAsync();

        // Act
        IResult<int> result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenSuccessful_ShouldReturnSuccess()
    {
        // Act
        IResult<int> result = await Act();

        // Assert
        AssertSuccess(result, TestResultValue);
    }

    [Test]
    public async Task WhenCanceledDuring_ShouldReturnCanceled()
    {
        // Arrange
        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        LoggerMock.IsEnabled();

        LoggerMock.LogQueryCanceledDuringExecution(QueryName);

        // Act
        IResult<int> result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        _testInternalExecuteAsync = (_, _) => throw exception;

        LoggerMock.IsEnabled();

        LoggerMock.LogQueryFailed(QueryName, exception);

        // Act
        IResult<int> result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        IResult result = QueryHandler.TestDomainError(testErrorMessage);

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        IResult result = QueryHandler.TestConflict(testErrorMessage);

        // Assert
        AssertConflict(result, testErrorMessage);
    }

    [Test]
    public void Canceled()
    {
        // Act
        IResult result = QueryHandler.TestCanceled();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public void NotFound()
    {
        // Act
        IResult result = QueryHandler.TestNotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void InternalError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        IResult result = QueryHandler.TestInternalError(testErrorMessage);

        // Assert
        AssertInternalError(result, testErrorMessage);
    }

    [Test]
    public void InternalErrorWithException()
    {
        // Arrange
        var testException = new Exception("Test Exception Message");

        // Act
        IResult result = QueryHandler.TestInternalError(testException);

        // Assert
        AssertInternalError(result, testException);
    }

    [Test]
    public void Invalid()
    {
        // Arrange
        ValidationState testValidationState = new();

        // Act
        IResult result = QueryHandler.TestInvalid(testValidationState);

        // Assert
        AssertInvalid(result, testValidationState);
    }

    [Test]
    public void Success()
    {
        // Arrange
        const int testValue = 123;

        // Act
        IResult<int> result = QueryHandler.TestSuccess(testValue);

        // Assert
        AssertSuccess(result, testValue);
    }
}
