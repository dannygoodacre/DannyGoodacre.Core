using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;

namespace DannyGoodacre.Cqrs.Tests;

public sealed class VoidTransactedStateCommandDecoratorTests
{
    public sealed class Command : ICommand;

    public sealed class TransactedStateCommand : ITransactedStateCommand;

    private readonly TransactionCommandTestContext<TransactedStateCommand> _transactionCommandContext = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenCommandIsNotITransactedStateCommand_BypassesTransactionSaveStateAndHook()
    {
        // Arrange
        var context = new TransactionCommandTestContext<Command>(MockBehavior.Strict);

        var command = new Command();

        var successResult = Result.Success();

        context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var pipeline = context.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        context.TransactionUnitMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()), Times.Never);

        context.PostCommitHookMock.Verify(x => x.ExecuteAsync(It.IsAny<Command>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerHandlerFails_DoesNotSaveStateOrExecuteHook()
    {
        // Arrange
        var command = new TransactedStateCommand();

        var failureResult = Result.Conflict("Database validation error");

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>((action, ct) => action(ct));

        _transactionCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        var pipeline = _transactionCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeConflict("Database validation error");

        _transactionCommandContext.TransactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _transactionCommandContext.PostCommitHookMock.Verify(x => x.ExecuteAsync(It.IsAny<TransactedStateCommand>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTransactionExecutionThrowsOperationCanceled_LogsAndReturnsCanceled()
    {
        // Arrange
        var command = new TransactedStateCommand();

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _transactionCommandContext.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeCanceled();

        _transactionCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.TransactionCanceled, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenTransactionExecutionThrowsUnhandledException_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new TransactedStateCommand();

        var dbException = new InvalidOperationException("Transaction aborted");

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbException);

        var pipeline = _transactionCommandContext.CreatePipeline();

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError(dbException);

        var faultLog = _transactionCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.TransactionFaulted, LogLevel.Error);

        Assert.Equal(dbException, faultLog.Exception);
    }

    [Fact]
    public async Task HandleAsync_WhenPostCommitHookIsNull_ExecutesTransactionSavesStateAndReturnsResult()
    {
        // Arrange
        var command = new TransactedStateCommand();

        var successResult = Result.Success();

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>((action, ct) => action(ct));

        _transactionCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var pipeline = _transactionCommandContext.CreatePipeline(includePostCommitHook: false);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _transactionCommandContext.TransactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenHookThrowsOperationCanceled_LogsMidExecutionCancellationAndSwallowsException()
    {
        // Arrange
        var command = new TransactedStateCommand();

        var successResult = Result.Success();

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>((action, ct) => action(ct));

        _transactionCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _transactionCommandContext.PostCommitHookMock
            .Setup(x => x.ExecuteAsync(command, successResult, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var pipeline = _transactionCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _transactionCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitHookCanceledMidExecution, LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WhenHookThrowsUnhandledException_LogsFaultAndReturnsInternalError()
    {
        // Arrange
        var command = new TransactedStateCommand();

        var successResult = Result.Success();

        var hookException = new InvalidOperationException("Event publication failed");

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>((action, ct) => action(ct));

        _transactionCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _transactionCommandContext.PostCommitHookMock
            .Setup(x => x.ExecuteAsync(command, successResult, It.IsAny<CancellationToken>()))
            .ThrowsAsync(hookException);

        var pipeline = _transactionCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInternalError(hookException);

        var faultLog = _transactionCommandContext.LogCollector.ShouldHaveLogged(CqrsLogEvent.PostCommitFaulted, LogLevel.Error);

        Assert.Equal(hookException, faultLog.Exception);
    }

    [Fact]
    public async Task HandleAsync_WhenAllSucceed_ExecutesTransactionSavesStateExecutesHookAndReturnsSuccess()
    {
        // Arrange
        var command = new TransactedStateCommand();

        var successResult = Result.Success();

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<IResult>>, CancellationToken>((action, ct) => action(ct));

        _transactionCommandContext.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _transactionCommandContext.TransactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _transactionCommandContext.PostCommitHookMock
            .Setup(x => x.ExecuteAsync(command, successResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pipeline = _transactionCommandContext.CreatePipeline(includePostCommitHook: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _transactionCommandContext.TransactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _transactionCommandContext.PostCommitHookMock.Verify(x => x.ExecuteAsync(command, successResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}
