using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.Data;
using Microsoft.Extensions.Logging;
using Moq;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class UnitOfWorkCommandHandlerValueTests : TestBase
{
    public class TestCommand : ICommand;

    private const string TestCommandName = "Test Unit Of Work Command With Value Handler";

    private static int _testExpectedChanges;

    private static int _testActualChanges;

    private const int TestResultValue = 789;

    private readonly CancellationToken _testCancellationToken = CancellationToken.None;

    private Mock<ILogger<TestUnitOfWorkCommandWithValueHandler>> _loggerMock = null!;

    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    private readonly TestCommand _testCommand = new();

    private static Func<TestCommand, CancellationToken, Task<Result<int>>> _internalExecuteAsync = null!;

    private static TestUnitOfWorkCommandWithValueHandler _testHandler = null!;

    public class TestUnitOfWorkCommandWithValueHandler(ILogger logger, IUnitOfWork unitOfWork)
        : UnitOfWorkCommandHandler<TestCommand, int>(logger, unitOfWork)
    {
        protected override string CommandName => TestCommandName;

        protected override int ExpectedChanges => _testExpectedChanges;

        protected override Task<Result<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => _internalExecuteAsync(command, cancellationToken);

        public Task<Result<int>> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<TestUnitOfWorkCommandWithValueHandler>>(MockBehavior.Strict);

        _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.Success(TestResultValue));

        _testExpectedChanges = -1;

        _testHandler = new TestUnitOfWorkCommandWithValueHandler(_loggerMock.Object, _unitOfWorkMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.InternalError(testError));

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndNotValidatingChanges_ShouldReturnSuccess()
    {
        // Arrange
        _testActualChanges = 456;

        SetupUnitOfWork_SaveChangesAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndChangesValid_ShouldReturnSuccess()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 123;

        SetupUnitOfWork_SaveChangesAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndInvalidChanges_ShouldReturnInternalError()
    {
        // Arrange
        _testExpectedChanges = 123;

        _testActualChanges = 456;

        SetupUnitOfWork_SaveChangesAsync();

        _loggerMock.Setup(LogLevel.Error, $"Command '{TestCommandName}' made an unexpected number of changes: Expected '{_testExpectedChanges}', Actual '{_testActualChanges}'.");

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, "Unexpected number of changes saved.");
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAndExceptionOccursDuringSaving_ShouldReturnInternalError()
    {
        // Arrange
        _testActualChanges = 456;

        const string testError = "Test Internal Error";

        var exception = new Exception(testError);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        _loggerMock.Setup(LogLevel.Critical, $"Command '{TestCommandName}' failed while saving changes, with exception: {testError}", exception: exception);

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    private Task<Result<int>> Act() => _testHandler.TestExecuteAsync(_testCommand, _testCancellationToken);

    private void SetupUnitOfWork_SaveChangesAsync()
        => _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ReturnsAsync(_testActualChanges)
            .Verifiable(Times.Once);
}
