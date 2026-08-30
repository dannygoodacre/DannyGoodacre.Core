using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class StateCommandHandlerBase<TCommand, TResult> : CommandHandlerBase<TCommand>
    where TCommand : ICommand
    where TResult : IResult
{
    internal StateCommandHandlerBase(ILogger logger, IStateUnit stateUnit) : base(logger)
    {
        StateUnit = stateUnit;
    }

    private IStateUnit StateUnit { get; }

    protected virtual Task AfterSaveAsync(TCommand command, TResult result, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    protected private async Task<TResult> BaseExecuteAsync(TCommand command,
                                                           Func<TCommand, CancellationToken, Task<TResult>> internalExecuteAsync,
                                                           Func<ValidationState, TResult> onInvalid,
                                                           Func<TResult> onCanceled,
                                                           Func<Error, TResult> onInternalError,
                                                           CancellationToken cancellationToken = default)
    {
        TResult result = await base.BaseExecuteAsync(command,
                                                     internalExecuteAsync,
                                                     onInvalid,
                                                     onCanceled,
                                                     onInternalError,
                                                     cancellationToken);

        if (result is not Success)
        {
            return result;
        }

        try
        {
            _ = await StateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogCommandCanceledWhilePersistingChanges(CommandName);

            return onCanceled();
        }
        catch (Exception ex)
        {
            Logger.LogCommandFailedWhilePersistingChanges(CommandName, ex);

            return onInternalError(ex.Message);
        }

        try
        {
            await AfterSaveAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogCommandCanceledDuringAfterSave(CommandName);

            return onCanceled();
        }
        catch (Exception ex)
        {
            Logger.LogCommandFailedDuringAfterSave(CommandName, ex);

            return onInternalError(ex.Message);
        }

        return result;
    }
}
