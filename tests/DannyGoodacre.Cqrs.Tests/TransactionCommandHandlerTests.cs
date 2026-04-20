using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

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

        protected override Task<Result> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _internalExecuteAsync(command, cancellationToken);

        public Task<Result> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Transaction Command Handler";

    private static int _testExpectedChanges;

    private static int _testActualChanges;

    private static Func<TestCommand, CancellationToken, Task<Result>> _internalExecuteAsync = null!;

    private readonly TestCommand _testCommand = new();

    protected override string CommandName => TestName;

    protected override Task<Result> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    protected override int TestActualChanges => _testActualChanges;

    [SetUp]
    public void SetUp()
    {
        _testExpectedChanges = -1;

        _internalExecuteAsync = (_, _) => Task.FromResult(Result.Success());

        CommandHandler = new TestTransactionCommandHandler(LoggerMock.Object, TransactionUnitMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldRollbackAndReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result.InternalError(testError));

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
        AssertInternalError(result, "Attempted to persist an unexpected number of changes.");
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
    public async Task ExecuteAsync_WhenSuccessfulAndCanceled_ShouldRollbackAndReturnCanceled()
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
        AssertCanceled(result);
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
