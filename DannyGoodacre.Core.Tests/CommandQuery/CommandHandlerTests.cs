using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class CommandHandlerTests : TestBase
{
    public class TestCommandRequest : ICommandRequest;

    private const string TestName = "Test Command Handler";

    private const string TestProperty = "Test Property";

    private const string TestError = "Test Error";

    private const string TestExceptionMessage = "Test Exception Message";

    private readonly CancellationToken _testCancellationToken = CancellationToken.None;

    private readonly TestCommandRequest _testCommandRequest = new();

    private static Action<ValidationState, TestCommandRequest> _validate = (_, _) => {};

    private static Func<TestCommandRequest, CancellationToken, Task<Result>> _internalExecuteAsync = (_, _) => Task.FromResult(new Result());

    private Mock<ILogger<TestCommandHandler>> _loggerMock = null!;

    public class TestCommandHandler(ILogger logger) : CommandHandler<TestCommandRequest>(logger)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommandRequest commandRequest)
            => _validate(validationState, commandRequest);

        protected override Task<Result> InternalExecuteAsync(TestCommandRequest commandRequest, CancellationToken cancellationToken)
            => _internalExecuteAsync(commandRequest, cancellationToken);

        public Task<Result> TestExecuteAsync(TestCommandRequest commandRequest, CancellationToken cancellationToken)
            => ExecuteAsync(commandRequest, cancellationToken);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<TestCommandHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(LogLevel.Error, $"Command '{TestName}' failed validation: {TestProperty}:{Environment.NewLine}  - {TestError}");

        _validate = (validationState, _)
            => validationState.AddError(TestProperty, TestError);

        var handler = new TestCommandHandler(_loggerMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testCommandRequest, _testCancellationToken);

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelled_ShouldReturnCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        _loggerMock = new Mock<ILogger<TestCommandHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(LogLevel.Information, $"Command '{TestName}' was cancelled before execution.");

        var handler = new TestCommandHandler(_loggerMock.Object);

        await cancellationTokenSource.CancelAsync();

        // Act
        var result = await handler.TestExecuteAsync(_testCommandRequest, cancellationTokenSource.Token);

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledDuring_ShouldReturnCancelled()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<TestCommandHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(LogLevel.Information, $"Command '{TestName}' was cancelled during execution.");

        _internalExecuteAsync = (_, _) => throw new OperationCanceledException();

        var handler = new TestCommandHandler(_loggerMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testCommandRequest, _testCancellationToken);

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        var exception = new ApplicationException(TestExceptionMessage);

        _loggerMock = new Mock<ILogger<TestCommandHandler>>(MockBehavior.Strict);

        _loggerMock.Setup(
            LogLevel.Critical,
            $"Command '{TestName}' failed with exception: {TestExceptionMessage}",
            exception: exception);

        _internalExecuteAsync = (_, _) => throw exception;

        var handler = new TestCommandHandler(_loggerMock.Object);

        // Act
        var result = await handler.TestExecuteAsync(_testCommandRequest, _testCancellationToken);

        // Assert
        AssertInternalError(result, TestExceptionMessage);
    }
}
