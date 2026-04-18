using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

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

        protected override Task<Result<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test State Command Handler";

    private const int TestResultValue = 123;

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<Result<int>>> _testInternalExecuteAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<Result<int>> Act() => CommandHandler.ExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result.Success(TestResultValue));

        CommandHandler = new TestStateCommandHandler(LoggerMock.Object, StateUnitMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessful_ShouldSaveChangesAndReturnSuccess()
    {
        // Arrange
        SetupStateUnit_SaveChangesAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result, TestResultValue);
    }

    [Test]
    public async Task ExecuteAsync_WhenCanceledWhilePersistingChanges_ShouldReturnCanceled()
    {
        // Arrange
        StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(new OperationCanceledException())
            .Verifiable(Times.Once);

        SetupLogger_CanceledWhilePersistingChanges();

        // Act
        var result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccursWhilePersistingChanges_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        SetupLogger_FailedWhilePersistingChanges(exception);

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }
}
