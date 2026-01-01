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

    private const int TestExpectedChanges = 123;

    private readonly CancellationToken _testCancellationToken = CancellationToken.None;

    private Mock<ILogger<TestUnitOfWorkCommandHandler>> _loggerMock = null!;

    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    private readonly TestCommand _testCommand = new();

    private static Func<TestCommand, CancellationToken, Task<Result>> _internalExecuteAsync = null!;

    public class TestUnitOfWorkCommandHandler(ILogger logger, IUnitOfWork unitOfWork)
        : UnitOfWorkCommandHandler<TestCommand>(logger, unitOfWork)
    {
        protected override string CommandName => TestCommandName;

        protected override int ExpectedChanges => TestExpectedChanges;

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
    }

    [Test]
    public async Task ExecuteAsync_WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _internalExecuteAsync = (_, _) => Task.FromResult(Result.InternalError(testError));

        var handler = new TestUnitOfWorkCommandHandler(_loggerMock.Object, _unitOfWorkMock.Object);

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

        var handler = new TestUnitOfWorkCommandHandler(_loggerMock.Object, _unitOfWorkMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testCommand, _testCancellationToken);

        // Assert
        AssertSuccess(result);
    }
}
