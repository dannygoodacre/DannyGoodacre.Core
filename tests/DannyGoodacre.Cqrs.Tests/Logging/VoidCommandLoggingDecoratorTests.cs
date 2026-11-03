namespace DannyGoodacre.Cqrs.Tests;

public sealed class VoidCommandLoggingDecoratorTests
{
    public sealed class TestCommand : ICommand;

    private sealed class CommandLoggingTestContext<TCommand>(MockBehavior mockBehavior = MockBehavior.Default)
        : TestContext<TCommand>
        where TCommand : class, ICommand
    {
        public Mock<ICommandHandler<TCommand>> InnerMock { get; } = new(mockBehavior);

        public ICommandHandler<TCommand> CreatePipeline()
            => new CommandLoggingDecorator<TCommand>(InnerMock.Object, Logger);
    }

    private readonly CommandLoggingTestContext<TestCommand> _context = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerSucceeds_LogsStartedAndSucceededAndReturnsSuccess()
    {
        // Arrange
        var command = new TestCommand();

        var expectedResult = Result.Success();

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var pipeline = _context.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);
        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestSucceeded, LogLevel.Information);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerFails_LogsStartedAndFailedAndReturnsFailure()
    {
        // Arrange
        var command = new TestCommand();

        const string expectedMessage = "Already exists";

        var expectedResult = Result.Conflict(expectedMessage);

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var pipeline = _context.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeConflict(expectedMessage);

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);
        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestFailed, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenCancelledPreExecution_LogsPreExecutionCancellationAndShortCircuits()
    {
        // Arrange
        var command = new TestCommand();

        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var pipeline = _context.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, cts.Token);

        // Assert
        result.ShouldBeCanceled();

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);
        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestCanceledPreExecution, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenCallerCancelsDuringExecution_LogsMidExecutionCancellationAndReturnsCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var command = new TestCommand();

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _context.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, cts.Token);

        // Assert
        result.ShouldBeCanceled();

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);
        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestCanceledMidExecution, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerThrowsOperationCanceledWithoutCallerCancellation_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new TestCommand();

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _context.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError();

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);
        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestFaulted, LogLevel.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerThrowsUnhandledException_LogsFaultWithExceptionAndReturnsInternalError()
    {
        // Arrange
        var command = new TestCommand();

        var expectedException = new InvalidOperationException("Exception message");

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var pipeline = _context.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError(expectedException);

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestStarted, LogLevel.Information);

        var faultLog = _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestFaulted, LogLevel.Error);

        Assert.Equal(expectedException, faultLog.Exception);
    }
}
