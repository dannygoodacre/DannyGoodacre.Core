using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class CommandHandlerTests : CommandHandlerTestBase<CommandHandlerTests.TestCommandHandler>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestCommandHandler(ILogger logger) : CommandHandler<TestCommand>(logger)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommand command)
            => _testValidate(validationState, command);

        protected override Task<Result> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);

        public Task<Result> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);

        public Result TestInvalid(ValidationState validationState)
            => Invalid(validationState);

        public Result TestDomainError(string error)
            => DomainError(error);

        public Result TestConflict(string error)
            => Conflict(error);

        public Result TestCanceled()
            => Canceled();

        public Result TestNotFound()
            => NotFound();

        public Result TestInternalError(string error)
            => InternalError(error);

        public Result TestInternalError(Exception exception)
            => InternalError(exception);
    }

    private const string TestName = "Test Command Handler";

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<Result>> _testInternalExecuteAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<Result> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result.Success());

        CommandHandler = new TestCommandHandler(LoggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"{testProperty}:{Environment.NewLine}  - {testError}");

        // Act
        Result result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCanceledBefore_ShouldReturnCanceled()
    {
        // Arrange
        CancellationTokenSource cancellationTokenSource = new();

        TestCancellationToken = cancellationTokenSource.Token;

        SetupLogger_IsEnabled();

        SetupLogger_CanceledBeforeExecution();

        await cancellationTokenSource.CancelAsync();

        // Act

        Result result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCanceledDuring_ShouldReturnCanceled()
    {
        // Arrange
        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        SetupLogger_IsEnabled();

        SetupLogger_CanceledDuringExecution();

        // Act
        Result result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        Exception exception = new(testExceptionMessage);

        _testInternalExecuteAsync = (_, _) => throw exception;

        SetupLogger_IsEnabled();

        SetupLogger_Failed(exception);

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public void Invalid()
    {
        // Arrange
        ValidationState testValidationState = new();

        // Act
        Result result = CommandHandler.TestInvalid(testValidationState);

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        Result result = CommandHandler.TestDomainError(testErrorMessage);

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        Result result = CommandHandler.TestConflict(testErrorMessage);

        // Assert
        AssertConflict(result, testErrorMessage);
    }

    [Test]
    public void Canceled()
    {
        // Act
        Result result = CommandHandler.TestCanceled();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public void NotFound()
    {
        // Act
        Result result = CommandHandler.TestNotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void InternalError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        Result result = CommandHandler.TestInternalError(testErrorMessage);

        // Assert
        AssertInternalError(result, testErrorMessage);
    }

    [Test]
    public void InternalErrorWithException()
    {
        // Arrange
        Exception testException = new("Test Exception Message");

        // Act
        Result result = CommandHandler.TestInternalError(testException);

        // Assert
        AssertInternalError(result, testException);
    }
}
