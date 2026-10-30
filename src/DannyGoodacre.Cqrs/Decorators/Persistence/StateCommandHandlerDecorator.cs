using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public interface IStateCommand;

public sealed class StateCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger,
    IPostCommitHook<TCommand>? postCommitHook = null) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedCommand)
        {
            return result;
        }

        string commandName = typeof(TCommand).Name;

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledWhilePersistingChanges(commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedWhilePersistingChanges(commandName, ex);

            return new InternalError(ex);
        }

        if (postCommitHook is null)
        {
            return result;
        }

        try
        {
            await postCommitHook.AfterSaveAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledDuringAfterSave(commandName);
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedDuringAfterSave(commandName, ex);

            return new InternalError(ex);
        }

        return result;
    }
}

public sealed class StateCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger,
    IPostCommitHook<TCommand, TResult>? postCommitHook = null) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : notnull
{
    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedCommand)
        {
            return result;
        }

        string commandName = typeof(TCommand).Name;

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledWhilePersistingChanges(commandName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedWhilePersistingChanges(commandName, ex);

            return new InternalError<TResult>(ex);
        }

        if (postCommitHook is null)
        {
            return result;
        }

        try
        {
            await postCommitHook.AfterSaveAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledDuringAfterSave(commandName);
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedDuringAfterSave(commandName, ex);

            return new InternalError<TResult>(ex);
        }

        return result;
    }
}
