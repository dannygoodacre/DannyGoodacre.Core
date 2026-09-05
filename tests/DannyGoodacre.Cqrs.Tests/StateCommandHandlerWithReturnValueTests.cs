namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class StateCommandHandlerWithReturnValueTests : StateCommandHandlerTestBase<StateCommandHandlerWithReturnValueTests.TestStateCommandHandler, int>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestStateCommandHandler(ILogger logger, IStateUnit stateUnit) : StateCommandHandler<TestCommand, int>(logger, stateUnit)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommand command)
            => _testValidate(validationState, command);

        protected override Task<IResult<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);

        protected override Task AfterSaveAsync(TestCommand command, int result, CancellationToken cancellationToken = default)
            => _testAfterSaveAsync(command, result, cancellationToken);

        public Task<IResult<int>> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test State Command Handler";

    private const int TestResultValue = 123;

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<IResult<int>>> _testInternalExecuteAsync = null!;

    private static Func<TestCommand, int, CancellationToken, Task> _testAfterSaveAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<IResult<int>> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult<IResult<int>>(new Success<int>(TestResultValue));

        _testAfterSaveAsync = (_, _, _) => Task.CompletedTask;

        CommandHandler = new TestStateCommandHandler(LoggerMock.Object, StateUnitMock.Object);
    }

    [Test]
    public async Task WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        _testInternalExecuteAsync =  (_, _) => Task.FromResult<IResult<int>>(new InternalError<int>(testErrorMessage));

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, testErrorMessage);
    }

    [Test]
    public async Task WhenCanceledWhilePersistingChanges_ShouldReturnCanceled()
    {
        // Arrange
        StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(new OperationCanceledException())
            .Verifiable(Times.Once);

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandCanceledWhilePersistingChanges(CommandName);

        // Act
        IResult result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenExceptionOccursWhilePersistingChanges_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandFailedWhilePersistingChanges(CommandName, exception);

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public async Task WhenCanceledDuringAfterSave_ShouldReturnCanceled()
    {
        // Arrange
        _testAfterSaveAsync = (_, _, _) => Task.FromException(new OperationCanceledException());

        SetupStateUnit_SaveChangesAsync();

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandCanceledDuringAfterSave(CommandName);

        // Act
        IResult result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenExceptionOccursDuringAfterSave_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        _testAfterSaveAsync = (_, _, _) => Task.FromException(exception);

        SetupStateUnit_SaveChangesAsync();

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandFailedDuringAfterSave(CommandName, exception);

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public async Task WhenSuccess_ShouldReturnSuccessfulResult()
    {
        // Arrange
        SetupStateUnit_SaveChangesAsync();

        // Act
        IResult result = await Act();

        // Assert
        AssertSuccess(result);
    }
}
