namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class TransactionCommandHandlerTests : TransactionCommandHandlerTestBase<TransactionCommandHandlerTests.TestTransactionCommandHandler>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestTransactionCommandHandler(ILogger logger, ITransactionUnit transactionUnit)
        : TransactionCommandHandler<TestCommand>(logger, transactionUnit)
    {
        protected override string CommandName => TestName;

        protected override int ExpectedChanges => _testExpectedChanges;

        protected override Task<IResult> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _internalExecuteAsync(command, cancellationToken);

        protected override Task AfterSaveAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testAfterSaveAsync(command, cancellationToken);

        public Task<IResult> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Transaction Command Handler";

    private static int _testExpectedChanges;

    private static int _testActualChanges;

    private static Func<TestCommand, CancellationToken, Task<IResult>> _internalExecuteAsync = null!;

    private static Func<TestCommand, CancellationToken, Task> _testAfterSaveAsync = null!;

    private readonly TestCommand _testCommand = new();

    protected override string CommandName => TestName;

    protected override Task<IResult> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    protected override int TestActualChanges => _testActualChanges;

    [SetUp]
    public void SetUp()
    {
        _testExpectedChanges = -1;

        _internalExecuteAsync = (_, _) => Task.FromResult<IResult>(new Success());

        _testAfterSaveAsync = (_, _) => Task.CompletedTask;

        CommandHandler = new TestTransactionCommandHandler(LoggerMock.Object, TransactionUnitMock.Object);
    }

    [Test]
    public async Task WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult<IResult>(new InternalError(testError));

        SetupTransactionUnit_ExecuteInTransactionAsync();

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task WhenSuccessfulAndInvalidNumberOfChanges_ShouldReturnInternalError()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 456;

        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandUnexpectedNumberOfChanges(CommandName, _testExpectedChanges, _testActualChanges);

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, "Attempted to persist an unexpected number of changes.");
    }

    [Test]
    public async Task WhenSuccessfulAndValidNumberOfChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 123;

        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        // Act
        IResult result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task WhenSuccessfulAndNotValidatingChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        // Act
        IResult result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task WhenCanceledWhilePersistingChanges_ShouldReturnCanceled()
    {
        // Arrange
        TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult>>>(),
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
        const string testExceptionMessage = "Test Persistence Exception";

        var exception = new Exception(testExceptionMessage);

        SetupTransactionUnit_ExecuteInTransactionAsync();

        TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult>>>(),
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
        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        _testAfterSaveAsync = (_, _) => Task.FromException(new OperationCanceledException());

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

        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        _testAfterSaveAsync = (_, _) => Task.FromException(exception);

        LoggerMock.IsEnabled();

        LoggerMock.LogCommandFailedDuringAfterSave(CommandName, exception);

        // Act
        IResult result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }
}
