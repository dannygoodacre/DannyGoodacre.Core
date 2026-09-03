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

        protected override Task<IResult> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);

        public Task<IResult> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);

        public IResult TestCanceled() => Canceled();

        public IResult TestConflict(string message) => Conflict(message);

        public IResult TestDomainError(string message) => DomainError(message);

        public IResult TestInternalError(Error error) => InternalError(error);

        public IResult TestInvalid(ValidationState validationState) => Invalid(validationState);

        public IResult TestNotFound() => NotFound();

        public IResult TestSuccess() => Success();
    }

    private const string TestName = "Test Command Handler";

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<IResult>> _testInternalExecuteAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<IResult> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult<IResult>(new Success());

        CommandHandler = new TestCommandHandler(LoggerMock.Object);
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

        LoggerMock.LogCommandFailedValidation(CommandName, $"{testProperty}:{Environment.NewLine}  - {testError}");

        // Act
        IResult result = await Act();

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

        LoggerMock.LogCommandCanceledBeforeExecution(CommandName);

        await cancellationTokenSource.CancelAsync();

        // Act

        IResult result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenSuccessful_ShouldReturnSuccess()
    {
        // Act
        IResult result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task WhenCanceledDuring_ShouldReturnCanceled()
    {
        // Arrange
        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandCanceledDuringExecution(CommandName);

        // Act
        IResult result = await Act();

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

        LoggerMock.LogCommandFailed(CommandName, exception);

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public void Canceled()
    {
        // Act
        IResult result = CommandHandler.TestCanceled();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        IResult result = CommandHandler.TestConflict(testErrorMessage);

        // Assert
        AssertConflict(result, testErrorMessage);
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        IResult result = CommandHandler.TestDomainError(testErrorMessage);

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public void InternalError()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        IResult result = CommandHandler.TestInternalError(testErrorMessage);

        // Assert
        AssertInternalError(result, testErrorMessage);
    }

    [Test]
    public void InternalErrorWithException()
    {
        // Arrange
        var testException = new Exception("Test Exception Message");

        // Act
        IResult result = CommandHandler.TestInternalError(testException);

        // Assert
        AssertInternalError(result, testException);
    }

    [Test]
    public void Invalid()
    {
        // Arrange
        ValidationState testValidationState = new();

        // Act
        IResult result = CommandHandler.TestInvalid(testValidationState);

        // Assert
        AssertInvalid(result, testValidationState);
    }

    [Test]
    public void NotFound()
    {
        // Act
        IResult result = CommandHandler.TestNotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void Success()
    {
        // Act
        IResult result = CommandHandler.TestSuccess();

        AssertSuccess(result);
    }
}
