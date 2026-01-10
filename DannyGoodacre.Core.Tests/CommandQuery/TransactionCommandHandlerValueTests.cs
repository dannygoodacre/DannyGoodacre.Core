using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class TransactionCommandWithValueHandlerTests : TestBase
{
    public class TestCommand : ICommandRequest;

    public class TestTransactionCommandHandler(ILogger logger, IUnitOfWork unitOfWork)
        : TransactionCommandHandler<TestCommand, int>(logger, unitOfWork)
    {
        protected override string CommandName => TestName;

        protected override int ExpectedChanges => _testExpectedChanges;

        protected override Task<Result<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => _internalExecuteAsync(command, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Transaction Command Handler";

    private static int _testResultValue;

    private static int _testExpectedChanges;

    private static int _testActualChanges;

    private CancellationToken _testCancellationToken;

    private readonly TestCommand _testCommand = new();

    private Mock<ILogger<TestTransactionCommandHandler>> _loggerMock = null!;

    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    private Mock<ITransaction> _testTransaction = null!;

    private static Func<TestCommand, CancellationToken, Task<Result<int>>> _internalExecuteAsync = null!;

    private static TestTransactionCommandHandler _testHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _testResultValue = 123;

        _testExpectedChanges = -1;

        _testCancellationToken = CancellationToken.None;

        _loggerMock = new Mock<ILogger<TestTransactionCommandHandler>>(MockBehavior.Strict);

        _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

        _testTransaction = new Mock<ITransaction>();

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.Success(_testResultValue));

        _testHandler = new TestTransactionCommandHandler(_loggerMock.Object, _unitOfWorkMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldRollbackAndReturnResult()
    {
        // Arrange
        SetupUnitOfWork_BeginTransactionAsync();

        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.InternalError(testError));

        SetupTransaction_RollbackAsync();

        SetupTransaction_DisposeAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndInvalidNumberOfChanges_ShouldRollbackAndReturnInternalError()
    {
        // Arrange
        SetupUnitOfWork_BeginTransactionAsync();

        _testExpectedChanges = 123;

        _testActualChanges = 456;

        SetupUnitOfWork_SaveChangesAsync();

        SetupTransaction_RollbackAsync();

        _loggerMock.Setup(LogLevel.Error, $"Command '{TestName}' attempted to persist an unexpected number of changes: Expected '{_testExpectedChanges}', Actual '{_testActualChanges}'.");

        SetupTransaction_DisposeAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, "Database integrity check failed.");

    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndValidNumberOfChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        SetupUnitOfWork_BeginTransactionAsync();

        _testExpectedChanges = 123;

        _testActualChanges = 123;

        SetupUnitOfWork_SaveChangesAsync();

        SetupTransaction_CommitAsync();

        SetupTransaction_DisposeAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result, _testResultValue);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndNotValidatingChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        SetupUnitOfWork_BeginTransactionAsync();

        SetupUnitOfWork_SaveChangesAsync();

        SetupTransaction_CommitAsync();

        SetupTransaction_DisposeAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result, _testResultValue);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelled_ShouldRollbackAndReturnCancelled()
    {
        // Arrange
        SetupUnitOfWork_BeginTransactionAsync();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ThrowsAsync(new OperationCanceledException())
            .Verifiable(Times.Once);

        SetupTransaction_RollbackAsync();

        _loggerMock.Setup(LogLevel.Information, $"Command '{TestName}' was cancelled while persisting changes.");

        SetupTransaction_DisposeAsync();

        // Act
        var result = await Act();

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndExceptionOccurs_ShouldRollbackAndReturnInternalError()
    {
        // Arrange
        SetupUnitOfWork_BeginTransactionAsync();

        const string testError = "Test Internal Error";

        var exception = new Exception(testError);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        SetupTransaction_RollbackAsync();

        _loggerMock.Setup(LogLevel.Critical, $"Command '{TestName}' experienced a transaction failure: {testError}", exception: exception);

        SetupTransaction_DisposeAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    private Task<Result<int>> Act() => _testHandler.TestExecuteAsync(_testCommand, _testCancellationToken);

    private void SetupUnitOfWork_BeginTransactionAsync()
        => _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ReturnsAsync(_testTransaction.Object)
            .Verifiable(Times.Once);

    private void SetupUnitOfWork_SaveChangesAsync()
        => _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ReturnsAsync(_testActualChanges)
            .Verifiable(Times.Once);

    private void SetupTransaction_CommitAsync()
        => _testTransaction
            .Setup(x => x.CommitAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    private void SetupTransaction_RollbackAsync()
        => _testTransaction
            .Setup(x => x.RollbackAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    private void SetupTransaction_DisposeAsync()
        => _testTransaction
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable(Times.Once);
}
