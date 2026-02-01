using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class PersistenceCommandHandler<TCommand, TResult>(ILogger logger, IUnitOfWork unitOfWork)
    : CommandHandler<TCommand, TResult>(logger) where TCommand : ICommand
{
    protected async override Task<Result<TResult>> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.ExecuteAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Command '{Command}' was cancelled while persisting changes.", CommandName);

            return Result.Cancelled();
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Command '{Command}' failed while persisting changes: {Exception}", CommandName, ex.Message);

            return Result.InternalError(ex.Message);
        }
    }
}
