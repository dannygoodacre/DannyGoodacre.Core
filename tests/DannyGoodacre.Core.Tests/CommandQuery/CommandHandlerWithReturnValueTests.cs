namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public sealed class CommandHandlerWithReturnValueTests : CommandHandlerTestBase<CommandHandlerWithReturnValueTests.TestCommandHandler, int>
{
    public sealed class TestCommand : ICommand;

    public sealed class TestCommandHandler(ILogger logger) : CommandHandler<TestCommand, int>(logger)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommand command)
            => _testValidate(validationState, command);

        protected override Task<Result<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Command Handler";

    private const int TestResultValue = 123;

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<Result<int>>> _testInternalExecuteAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<Result<int>> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result.Success(TestResultValue));

        CommandHandler = new TestCommandHandler(LoggerMock.Object);
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

        TestCancellationToken = cancellationTokenSource.Token;

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
