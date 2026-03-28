namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public sealed class TransactionCommandHandlerWithReturnValueTests : TransactionCommandHandlerTestBase<TransactionCommandHandlerWithReturnValueTests.TestTransactionCommandHandler, int>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestTransactionCommandHandler(ILogger logger, ITransactionUnit transactionUnit)
        : TransactionCommandHandler<TestCommand, int>(logger, transactionUnit)
    {
        protected override string CommandName => TestName;

        protected new int ExpectedChanges = _testExpectedChanges;

        protected override Task<Result<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _internalExecuteAsync(command, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Transaction Command Handler";

    private const int TestResultValue = 123;

    private static int _testExpectedChanges;

    private static int _testActualChanges;

    private static Func<TestCommand, CancellationToken, Task<Result<int>>> _internalExecuteAsync = null!;

    private readonly TestCommand _testCommand = new();

    protected override string CommandName => TestName;

    protected override Task<Result<int>> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    protected override int TestActualChanges => _testActualChanges;

    [SetUp]
    public void SetUp()
    {
        _testExpectedChanges = -1;

        _internalExecuteAsync = (_, _) => Task.FromResult(Result.Success(TestResultValue));

        CommandHandler = new TestTransactionCommandHandler(LoggerMock.Object, TransactionUnitMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldRollbackAndReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.InternalError(testError));

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndInvalidNumberOfChanges_ShouldRollbackAndReturnInternalError()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 456;

        SetupTransactionUnit_SaveChangesAsync();

        SetupTransaction_RollbackAsync();

        SetupLogger_UnexpectedNumberOfChanges(_testExpectedChanges, _testActualChanges);

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, "State integrity check failed.");

    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndValidNumberOfChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 123;

        Setup_SaveChangesAndCommitAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndNotValidatingChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        Setup_SaveChangesAndCommitAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndCanceled_ShouldRollbackAndReturnCancelled()
    {
        // Arrange
        TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(new OperationCanceledException())
            .Verifiable(Times.Once);

        SetupTransaction_RollbackAsync();

        SetupLogger_CanceledDuringRollback();

        // Act
        var result = await Act();

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndExceptionOccurs_ShouldRollbackAndReturnInternalError()
    {
        // Arrange
        const string testError = "Test Internal Error";

        var exception = new Exception(testError);

        TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        SetupTransaction_RollbackAsync();

        SetupLogger_TransactionFailure(exception);

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }
}
