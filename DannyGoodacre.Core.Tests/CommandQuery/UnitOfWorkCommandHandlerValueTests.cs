using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.Data;
using Microsoft.Extensions.Logging;
using Moq;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class UnitOfWorkCommandHandlerValueTests : TestBase
{
    public class TestCommand : ICommand;

    private const string TestCommandName = "Test Unit Of Work Command Handler";

    private const int TestExpectedChanges = 123;

    private const int TestResultValue = 789;

    private readonly CancellationToken _testCancellationToken = CancellationToken.None;

    private Mock<ILogger<TestUnitOfWorkCommandWithValueHandler>> _loggerMock = null!;

    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    private readonly TestCommand _testCommand = new();

    private static Func<TestCommand, CancellationToken, Task<Result<int>>> _internalExecuteAsync = null!;

    public class TestUnitOfWorkCommandWithValueHandler(ILogger logger, IUnitOfWork unitOfWork)
        : UnitOfWorkCommandHandler<TestCommand, int>(logger, unitOfWork)
    {
        protected override string CommandName => TestCommandName;

        protected override int ExpectedChanges => TestExpectedChanges;

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
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result<int>.InternalError(testError));

        var handler = new TestUnitOfWorkCommandWithValueHandler(_loggerMock.Object, _unitOfWorkMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testCommand, _testCancellationToken);

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessfulAnd_ShouldReturnSuccess()
    {
        // Arrange
        const int actualChanges = 456;

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == _testCancellationToken)))
            .ReturnsAsync(actualChanges)
            .Verifiable(Times.Once);

        _loggerMock.Setup(LogLevel.Error, $"Command '{TestCommandName}' made an unexpected number of changes: Expected '{TestExpectedChanges}', Actual '{actualChanges}'.");

        var handler = new TestUnitOfWorkCommandWithValueHandler(_loggerMock.Object, _unitOfWorkMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testCommand, _testCancellationToken);

        // Assert
        AssertSuccess(result);
    }
}
