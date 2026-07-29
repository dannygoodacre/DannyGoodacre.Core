using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class TransactionCommandHandlerWithReturnValueTests : TransactionCommandHandlerTestBase<TransactionCommandHandlerWithReturnValueTests.TestTransactionCommandHandler, int>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestTransactionCommandHandler(ILogger logger, ITransactionUnit transactionUnit)
        : TransactionCommandHandler<TestCommand, int>(logger, transactionUnit)
    {
        protected override string CommandName => TestName;

        protected override int ExpectedChanges => _testExpectedChanges;

        protected override Task<Result<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _internalExecuteAsync(command, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
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
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.InternalError(testError));

        SetupTransactionUnit_ExecuteInTransactionAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndInvalidNumberOfChanges_ShouldReturnInternalError()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 456;

        SetupLogger_IsEnabled();

        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        SetupLogger_UnexpectedNumberOfChanges(_testExpectedChanges, _testActualChanges);

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, "Attempted to persist an unexpected number of changes.");
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndValidNumberOfChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 123;

        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndNotValidatingChanges_ShouldCommitAndReturnSuccess()
    {
        // Arrange
        SetupTransactionUnit_ExecuteInTransactionAsync();

        SetupTransactionUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        const string testError = "Test Internal Error";

        Exception exception = new(testError);

        SetupLogger_IsEnabled();

        SetupTransactionUnit_ExecuteInTransactionAsync();

        TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        SetupLogger_Failed(exception);

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }
}
