using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class StateCommandHandlerBase<TCommand, TResult> : CommandHandlerBase<TCommand, TResult>
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

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic.
    /// If the command succeeds, persist all state changes and call <see cref="AfterSaveAsync"/>.
    /// </summary>
    /// <inheritdoc cref="CommandHandlerBase{TCommand,TResult}.ExecuteAsync" />
    protected new async Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        TResult result = await base.ExecuteAsync(command, cancellationToken);

        if (result.IsFailure)
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

            return Canceled();
        }
        catch (Exception ex)
        {
            Logger.LogCommandFailedWhilePersistingChanges(CommandName, ex);

            return InternalError(ex.Message);
        }

        try
        {
            await AfterSaveAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogCommandCanceledDuringAfterSave(CommandName);

            return Canceled();
        }
        catch (Exception ex)
        {
            Logger.LogCommandFailedDuringAfterSave(CommandName, ex);

            return InternalError(ex.Message);
        }

        return result;
    }
}
