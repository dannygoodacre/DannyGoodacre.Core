namespace DannyGoodacre.Cqrs.Tests;

public sealed class VoidStateCommandHandlerDecoratorTests
{
    public sealed class Command : ICommand;

    public sealed class StateCommand : IStateCommand;

    public sealed class TransactedStateCommand : ITransactedStateCommand;

    private readonly StateCommandTestContext<StateCommand> _stateCommandContext = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerFails_DoesNotSaveStateOrExecuteHook()
    {
        // Arrange
        var command = new StateCommand();

        var failureResult = new Conflict("Database validation error");

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeConflict("Database validation error");

        _stateCommandContext.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _stateCommandContext.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<StateCommand>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsNotIStateCommand_BypassesSaveStateAndHook()
    {
        // Arrange
        var context = new StateCommandTestContext<Command>(MockBehavior.Strict);

        var command = new Command();

        var successResult = new Success();

        context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var pipeline = context.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        context.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        context.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<Command>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsITransactedCommand_BypassesSaveStateAndHook()
    {
        // Arrange
        var context = new StateCommandTestContext<TransactedStateCommand>(MockBehavior.Strict);

        var command = new TransactedStateCommand();

        var successResult = new Success();

        context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var pipeline = context.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        context.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        context.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<TransactedStateCommand>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveChangesIsCanceledByCaller_LogsAndReturnsCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var command = new StateCommand();

        var successResult = new Success();

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var pipeline = _stateCommandContext.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, cts.Token);

        // Assert
        result.ShouldBeCanceled();

        _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.SaveChangesCanceled, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveChangesThrowsOperationCanceledWithoutCallerCancellation_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _stateCommandContext.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError();

        _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.SaveChangesFaulted, LogLevel.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveChangesThrowsUnhandledException_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        var exception = new InvalidOperationException("DB deadlock");

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var pipeline = _stateCommandContext.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError(exception);

        var faultLog = _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.SaveChangesFaulted, LogLevel.Error);

        Assert.Equal(exception, faultLog.Exception);
    }

    [Fact]
    public async Task HandleAsync_WhenPostCommitHookIsNull_SavesStateAndReturnsResult()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: false);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _stateCommandContext.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenCanceledAfterSave_StillRunsHookWithoutTheCallerToken()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Token cancelled before hook runs

        _stateCommandContext.PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, successResult, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, cts.Token);

        // Assert
        result.ShouldBeSuccess();


        // The data is committed, so the hook runs with CancellationToken.None.
        _stateCommandContext.PostCommitHookMock.Verify(x => x.AfterCommitAsync(command, successResult, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenHookThrowsOperationCanceled_LogsMidExecutionCancellationAndSwallowsException()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _stateCommandContext.PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, successResult, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitHookCanceledMidExecution, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenHookThrowsUnhandledException_LogsFaultAndReturnsOriginalResult()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        var hookException = new InvalidOperationException("Event publication failed");

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _stateCommandContext.PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, successResult, It.IsAny<CancellationToken>()))
            .ThrowsAsync(hookException);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        // The data is committed, so a hook failure does not change the result.
        result.ShouldBeSuccess();

        var faultLog = _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitFaulted, LogLevel.Error);

        Assert.Equal(hookException, faultLog.Exception);
    }

    [Fact]
    public async Task HandleAsync_WhenAllSucceed_SavesStateExecutesHookAndReturnsSuccess()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = new Success();

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _stateCommandContext.PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, successResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _stateCommandContext.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _stateCommandContext.PostCommitHookMock.Verify(x => x.AfterCommitAsync(command, successResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}
