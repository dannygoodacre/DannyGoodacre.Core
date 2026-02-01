using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract partial class PersistenceCommandHandlerBase<TCommand, TResult>
    : CommandHandlerBase<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    internal PersistenceCommandHandlerBase(ILogger logger, IUnitOfWork unitOfWork) : base(logger)
    {
        UnitOfWork = unitOfWork;
    }

    protected IUnitOfWork UnitOfWork { get; }

    protected new async Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.ExecuteAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                await UnitOfWork.SaveChangesAsync(cancellationToken);
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
    private static partial void LogFailedWhilePersistingChanges(ILogger logger, Exception ex, string command);
}
