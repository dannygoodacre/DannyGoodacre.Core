using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract partial class StateCommandHandlerBase<TCommand, TResult>
    : CommandHandlerBase<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    internal StateCommandHandlerBase(ILogger logger, IStateUnit stateUnit) : base(logger)
    {
        StateUnit = stateUnit;
    }

    private IStateUnit StateUnit { get; }

    protected virtual Task AfterSaveAsync(TCommand command, TResult result, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected async override Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        TResult result = await base.ExecuteAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result;
        }

        try
        {
            _ = await StateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogCanceledWhilePersistingChanges(CommandName);

            return Canceled();
        }
        catch (Exception ex)
        {
            Logger.LogFailedWhilePersistingChanges(ex, CommandName);

            return InternalError(ex.Message);
        }

        try
        {
            await AfterSaveAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogCanceledDuringAfterSave(CommandName);

            return Canceled();
        }
        catch (Exception ex)
        {
            Logger.LogFailedDuringAfterSave(ex, CommandName);

            return InternalError(ex.Message);
        }

        return result;
    }
}
