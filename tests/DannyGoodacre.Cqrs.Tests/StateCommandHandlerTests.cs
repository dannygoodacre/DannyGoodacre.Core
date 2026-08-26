using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class StateCommandHandlerTests : StateCommandHandlerTestBase<StateCommandHandlerTests.TestStateCommandHandler>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestStateCommandHandler(ILogger logger, IStateUnit stateUnit) : StateCommandHandler<TestCommand>(logger, stateUnit)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommand command)
            => _testValidate(validationState, command);

        protected override Task<Result> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);

        protected override Task AfterSaveAsync(TestCommand command, Result result, CancellationToken cancellationToken = default)
            => _testAfterSaveAsync(command, result, cancellationToken);

        public Task<Result> TestExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => ExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test State Command Handler";

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<Result>> _testInternalExecuteAsync = null!;

    private static Func<TestCommand, Result, CancellationToken, Task> _testAfterSaveAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<Result> Act() => CommandHandler.TestExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result.Success());

        _testAfterSaveAsync = (_, _, _) => Task.CompletedTask;

        CommandHandler = new TestStateCommandHandler(LoggerMock.Object, StateUnitMock.Object);
    }

    [Test]
    public async Task WhenNotSuccessful_ShouldReturnResult()
    {
        // Arrange
        const string testError = "Test Internal Error";

        _testInternalExecuteAsync =  (_, _) => Task.FromResult(Result.InternalError(testError));

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, testError);
    }

    [Test]
    public async Task WhenCanceledWhilePersistingChanges_ShouldReturnCanceled()
    {
        // Arrange
        StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(new OperationCanceledException())
            .Verifiable(Times.Once);

        LoggerMock.IsEnabled();

        LoggerMock.LogCanceledWhilePersistingChanges(CommandName);

        // Act
        Result result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenExceptionOccursWhilePersistingChanges_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        StateUnitMock
            .Setup(x => x.SaveChangesAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ThrowsAsync(exception)
            .Verifiable(Times.Once);

        LoggerMock.IsEnabled();

        LoggerMock.LogFailedWhilePersistingChanges(CommandName, exception);

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public async Task WhenCanceledDuringAfterSave_ShouldReturnCanceled()
    {
        // Arrange
        _testAfterSaveAsync = (_, _, _) => Task.FromException(new OperationCanceledException());

        SetupStateUnit_SaveChangesAsync();

        LoggerMock.IsEnabled();

        LoggerMock.LogCanceledDuringAfterSave(CommandName);

        // Act
        Result result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task WhenExceptionOccursDuringAfterSave_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        _testAfterSaveAsync = (_, _, _) => Task.FromException(exception);

        SetupStateUnit_SaveChangesAsync();

        LoggerMock.IsEnabled();

        LoggerMock.LogFailedDuringAfterSave(CommandName, exception);

        // Act
        Result result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    [Test]
    public async Task WhenSuccess_ShouldReturnSuccessfulResult()
    {
        // Arrange
        SetupStateUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }
}
