namespace DannyGoodacre.Cqrs.Tests;

public sealed class StateCommandDecoratorTests
{
    public sealed class Command : ICommand;

    public sealed class StateCommand : IStateCommand;

    public sealed class TransactedStateCommand : ITransactedStateCommand;

    private readonly StateCommandTestContext<StateCommand, Guid> _stateCommandContext = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerFails_DoesNotSaveStateOrExecuteHook()
    {
        // Arrange
        var command = new StateCommand();

        var failureResult = Result<Guid>.Conflict("Database validation error");

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeConflict("Database validation error");

        _stateCommandContext.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _stateCommandContext.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<StateCommand>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsNotIStateCommand_BypassesSaveStateAndHook()
    {
        // Arrange
        var context = new StateCommandTestContext<Command, Guid>(MockBehavior.Strict);

        var command = new Command();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

        context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var pipeline = context.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedId);

        context.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        context.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<Command>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsITransactedCommand_BypassesSaveStateAndHook()
    {
        // Arrange
        var context = new StateCommandTestContext<TransactedStateCommand, Guid>(MockBehavior.Strict);

        var command = new TransactedStateCommand();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

        context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var pipeline = context.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedId);

        context.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        context.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<TransactedStateCommand>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveChangesThrowsOperationCanceled_LogsAndReturnsCanceled()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = Result<Guid>.Success(Guid.NewGuid());

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _stateCommandContext.CreatePipeline();

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeCanceled();

        _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.SaveChangesCanceled, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveChangesThrowsUnhandledException_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new StateCommand();

        var successResult = Result<Guid>.Success(Guid.NewGuid());

        var dbException = new InvalidOperationException("DB deadlock");

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbException);

        var pipeline = _stateCommandContext.CreatePipeline();

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError(dbException);

        var faultLog = _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.SaveChangesFaulted, LogLevel.Error);

        Assert.Equal(dbException, faultLog.Exception);
    }

    [Fact]
    public async Task HandleAsync_WhenPostCommitHookIsNull_SavesStateAndReturnsResult()
    {
        // Arrange
        var command = new StateCommand();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: false);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedId);

        _stateCommandContext.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenCancelledPreHookExecution_LogsCancellationAndReturnsOriginalResult()
    {
        // Arrange
        var command = new StateCommand();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

        _stateCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _stateCommandContext.StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        using var cts = new CancellationTokenSource();

        await cts.CancelAsync();

        var pipeline = _stateCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, cts.Token);

        // Assert
        result.ShouldBeSuccess(expectedId);

        _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitHookCanceledPreExecution, LogLevel.Warning);

        _stateCommandContext.PostCommitHookMock.Verify(x => x.AfterCommitAsync(It.IsAny<StateCommand>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenHookThrowsOperationCanceled_LogsMidExecutionCancellationAndSwallowsException()
    {
        // Arrange
        var command = new StateCommand();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

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
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedId);

        _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitHookCanceledMidExecution, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenHookThrowsUnhandledException_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new StateCommand();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

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
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError(hookException);

        var faultLog = _stateCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitFaulted, LogLevel.Error);

        Assert.Equal(hookException, faultLog.Exception);
    }

    [Fact]
    public async Task HandleAsync_WhenAllSucceed_SavesStateExecutesHookAndReturnsSuccess()
    {
        // Arrange
        var command = new StateCommand();

        var expectedId = Guid.NewGuid();

        var successResult = Result<Guid>.Success(expectedId);

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
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedId);

        _stateCommandContext.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _stateCommandContext.PostCommitHookMock.Verify(x => x.AfterCommitAsync(command, successResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}
