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

    public async override Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            TResult result = await base.ExecuteAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                _ = await StateUnit.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            LogCanceledWhilePersistingChanges(Logger, CommandName);

            return MapResult(Result.Canceled());
        }
        catch (Exception ex)
        {
            LogFailedWhilePersistingChanges(Logger, ex, CommandName);

            return MapResult(Result.InternalError(ex.Message));
        }
    }

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled while persisting changes.")]
    private static partial void LogCanceledWhilePersistingChanges(ILogger logger, string command);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed while persisting changes.")]
    private static partial void LogFailedWhilePersistingChanges(ILogger logger, Exception exception, string command);
}
