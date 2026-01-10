using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Tests.CommandQuery;

[TestFixture]
public class CommandHandlerTests : TestBase
{
    public class TestCommandRequest : ICommandRequest;

    public class TestCommandHandler(ILogger logger) : CommandHandler<TestCommandRequest>(logger)
    {
        protected override string CommandName => TestName;

        protected override void Validate(ValidationState validationState, TestCommandRequest commandRequest)
            => _testValidate(validationState, commandRequest);

        protected override Task<Result> InternalExecuteAsync(TestCommandRequest commandRequest, CancellationToken cancellationToken)
            => _testInternalExecuteAsync(commandRequest, cancellationToken);

        public Task<Result> TestExecuteAsync(TestCommandRequest commandRequest, CancellationToken cancellationToken)
            => ExecuteAsync(commandRequest, cancellationToken);
    }

    private const string TestName = "Test Command Handler";

    private CancellationToken _testCancellationToken;

    private Mock<ILogger<TestCommandHandler>> _loggerMock = null!;

    private static Action<ValidationState, TestCommandRequest> _testValidate = null!;

    private static Func<TestCommandRequest, CancellationToken, Task<Result>> _testInternalExecuteAsync = null!;

    private static TestCommandRequest _testCommandRequest = null!;

    private static TestCommandHandler _testCommandHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _testCancellationToken = CancellationToken.None;

        _testValidate = (_, _) => {};

        _testInternalExecuteAsync = (_, _) => Task.FromResult(Result.Success());

        _loggerMock = new Mock<ILogger<TestCommandHandler>>(MockBehavior.Strict);

        _testCommandRequest = new TestCommandRequest();

        _testCommandHandler = new TestCommandHandler(_loggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnInvalid()
    {
        // Arrange
        const string testProperty = "Test Property";

        const string testError = "Test Error";

        _loggerMock.Setup(LogLevel.Error, $"Command '{TestName}' failed validation: {testProperty}:{Environment.NewLine}  - {testError}");

        _testValidate = (validationState, _) => validationState.AddError(testProperty, testError);

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelledBefore_ShouldReturnCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        _testCancellationToken = cancellationTokenSource.Token;

        _loggerMock.Setup(LogLevel.Information, $"Command '{TestName}' was cancelled before execution.");

        await cancellationTokenSource.CancelAsync();

        // Act
        var result = await Act();

        // Assert
        AssertCancelled(result);
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
    public async Task ExecuteAsync_WhenCancelledDuring_ShouldReturnCancelled()
    {
        // Arrange
        _loggerMock.Setup(LogLevel.Information, $"Command '{TestName}' was cancelled during execution.");

        _testInternalExecuteAsync = (_, _) => throw new OperationCanceledException();

        // Act
        var result = await Act();

        // Assert
        AssertCancelled(result);
    }

    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnInternalError()
    {
        // Arrange
        const string testExceptionMessage = "Test Exception Message";

        var exception = new Exception(testExceptionMessage);

        _loggerMock.Setup(LogLevel.Critical, $"Command '{TestName}' failed with exception: {testExceptionMessage}", exception: exception);

        _testInternalExecuteAsync = (_, _) => throw exception;

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, testExceptionMessage);
    }

    private Task<Result> Act() => _testCommandHandler.TestExecuteAsync(_testCommandRequest, _testCancellationToken);
}
