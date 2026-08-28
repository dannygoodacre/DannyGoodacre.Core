using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

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

        protected override Task<Result<int>> InternalExecuteAsync(TestQuery query, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(query, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestQuery query, CancellationToken cancellationToken = default)
            => ExecuteAsync(query, cancellationToken);

        public NUnit.Framework.Result TestInvalid(ValidationState validationState)
            => Invalid(validationState);

        public NUnit.Framework.Result TestDomainError(string error)
            => DomainError(error);

        public NUnit.Framework.Result TestConflict(string error)
            => Conflict(error);

        public NUnit.Framework.Result TestCanceled()
            => Canceled();

        public NUnit.Framework.Result TestNotFound()
            => NotFound();

        public NUnit.Framework.Result TestInternalError(string error)
            => InternalError(error);

        public NUnit.Framework.Result TestInternalError(Exception exception)
            => InternalError(exception);
    }

    private const string TestName = "Test Query Handler";

    private const int TestResultValue = 123;

    private static TestQuery _testQuery = null!;

    private static Action<ValidationState, TestQuery> _testValidate = null!;

    private static Func<TestQuery, CancellationToken, Task<Result<int>>> _testInternalExecuteAsync = null!;

    protected override string QueryName => TestName;

    protected override Task<Result<int>> Act() => QueryHandler.TestExecuteAsync(_testQuery, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result<int>.Success(TestResultValue));

        _testQuery = new TestQuery();

        QueryHandler = new TestQueryHandler(LoggerMock.Object);
    }

    [Test]
    public async Task WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        LoggerMock.IsEnabled();

        LoggerMock.LogQueryFailedValidation(QueryName, $"{testProperty}:{Environment.NewLine}  - {testError}");

        // Act
        Result<int> result = await Act();

        // Assert
        AssertInvalid(result);
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
        Result<int> result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenSuccessful_ShouldReturnSuccess()
    {
        // Act
        Result<int> result = await Act();

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
        Result<int> result = await Act();

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
        Result<int> result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public void Invalid()
    {
        // Arrange
        ValidationState testValidationState = new();

        // Act
        NUnit.Framework.Result result = QueryHandler.TestInvalid(testValidationState);

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        NUnit.Framework.Result result = QueryHandler.TestDomainError(testErrorMessage);

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        NUnit.Framework.Result result = QueryHandler.TestConflict(testErrorMessage);

        // Assert
        AssertConflict(result, testErrorMessage);
    }

    [Test]
    public void Canceled()
    {
        // Act
        NUnit.Framework.Result result = QueryHandler.TestCanceled();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public void NotFound()
    {
        // Act
        NUnit.Framework.Result result = QueryHandler.TestNotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void InternalError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        NUnit.Framework.Result result = QueryHandler.TestInternalError(testErrorMessage);

        // Assert
        AssertInternalError(result, testErrorMessage);
    }

    [Test]
    public void InternalErrorWithException()
    {
        // Arrange
        var testException = new Exception("Test Exception Message");

        // Act
        NUnit.Framework.Result result = QueryHandler.TestInternalError(testException);

        // Assert
        AssertInternalError(result, testException);
    }
}
