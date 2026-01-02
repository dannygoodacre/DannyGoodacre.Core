using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.Data;
using Microsoft.Extensions.Logging;
using Moq;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class UnitOfWorkCommandHandlerTests : TestBase
{
    public class TestCommand : ICommand;

    private const string TestCommandName = "Test Unit Of Work Command Handler";

    private static int _testExpectedChanges;

    private static int _testActualChanges;

    private readonly CancellationToken _testCancellationToken = CancellationToken.None;

    private Mock<ILogger<TestUnitOfWorkCommandHandler>> _loggerMock = null!;

    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    private readonly TestCommand _testCommand = new();

    private static Func<TestCommand, CancellationToken, Task<Result>> _internalExecuteAsync = null!;

    private static TestUnitOfWorkCommandHandler _testHandler = null!;

    public class TestUnitOfWorkCommandHandler(ILogger logger, IUnitOfWork unitOfWork)
        : UnitOfWorkCommandHandler<TestCommand>(logger, unitOfWork)
    {
        protected override string CommandName => TestCommandName;

        protected override int ExpectedChanges => _testExpectedChanges;

        protected override Task<Result> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => _internalExecuteAsync(command, cancellationToken);

        public Task<Result> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken)
            => ExecuteAsync(command, cancellationToken);
    }

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<TestUnitOfWorkCommandHandler>>(MockBehavior.Strict);

        _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

        _internalExecuteAsync = (_, _) => Task.FromResult(Result.Success());

        _testExpectedChanges = -1;

        _testHandler = new TestUnitOfWorkCommandHandler(_loggerMock.Object, _unitOfWorkMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result.InternalError(testError));

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

    private Task<Result> Act() => _testHandler.TestExecuteAsync(_testCommand, _testCancellationToken);

    private void SetupUnitOfWork_SaveChangesAsync()
        => _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ReturnsAsync(_testActualChanges)
            .Verifiable(Times.Once);
}
