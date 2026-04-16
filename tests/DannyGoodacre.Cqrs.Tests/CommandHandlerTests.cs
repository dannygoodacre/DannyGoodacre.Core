using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Tests;

[TestFixture]
public sealed class CommandHandlerTests : CommandHandlerTestBase<CommandHandlerTests.TestCommandHandler>
{
    public sealed record TestCommand : ICommand;

    public sealed class TestCommandHandler(ILogger logger) : CommandHandler<TestCommand>(logger)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommand command)
            => _testValidate(validationState, command);

        protected override Task<Result> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
            => _testInternalExecuteAsync(command, cancellationToken);
    }

    private const string TestName = "Test Command Handler";

    private static TestCommand _testCommand = null!;

    private static Action<ValidationState, TestCommand> _testValidate = null!;

    private static Func<TestCommand, CancellationToken, Task<Result>> _testInternalExecuteAsync = null!;

    protected override string CommandName => TestName;

    protected override Task<Result> Act() => CommandHandler.ExecuteAsync(_testCommand, TestCancellationToken);

    [SetUp]
    public void SetUp()
    {
        _testCommand = new TestCommand();

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result.Success());

        CommandHandler = new TestCommandHandler(LoggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        SetupLogger_FailedValidation($"{testProperty}:{Environment.NewLine}  - {testError}");

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCanceledBefore_ShouldReturnCanceled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        TestCancellationToken = cancellationTokenSource.Token;

        SetupLogger_CanceledBeforeExecution();

        await cancellationTokenSource.CancelAsync();

        // Act

        var result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCanceledDuring_ShouldReturnCanceled()
    {
        // Arrange
        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        SetupLogger_CanceledDuringExecution();

        // Act
        var result = await Act();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        _testInternalExecuteAsync = (_, _) => throw exception;

        SetupLogger_Failed(exception);

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }
}
