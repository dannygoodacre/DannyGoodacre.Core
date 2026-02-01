using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract partial class TransactionCommandHandlerBase<TCommand, TResult>(ILogger logger, IUnitOfWork unitOfWork)
    : PersistenceCommandHandlerBase<TCommand, TResult>(logger, unitOfWork)
    where TCommand : ICommand
    where TResult : Result
{
    /// <summary>
    /// The number of state entries expected to be persisted upon completion.
    /// </summary>
    /// <value>
    /// Defaults to -1 to disable validation.
    /// </value>
    /// <remarks>
    /// This is compared against the result of <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </remarks>
    protected virtual int ExpectedChanges => -1;

    protected new async Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await UnitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await base.ExecuteAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return result;
            }

            var actualChanges = await UnitOfWork.SaveChangesAsync(cancellationToken);

            if (ExpectedChanges != -1 && actualChanges != ExpectedChanges)
            {
                await transaction.RollbackAsync(cancellationToken);

                LogUnexpectedNumberOfChanges(Logger, CommandName, ExpectedChanges, actualChanges);

                return MapResult(Result.InternalError("Datastore integrity check failed."));
            }

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);

            LogCanceledDuringRollback(Logger, CommandName);

            return MapResult(Result.Canceled());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            LogTransactionFailure(Logger, ex, CommandName);

            return MapResult(Result.InternalError(ex.Message));
        }
    }

    [LoggerMessage(LogLevel.Error, "Command '{Command}' attempted to persist an unexpected number of changes: Expected '{Expected}', Actual '{Actual}'.")]
    private static partial void LogUnexpectedNumberOfChanges(ILogger logger, string command, int expected, int actual);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled while rolling back changes.")]
    private static partial void LogCanceledDuringRollback(ILogger logger, string command);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' experienced a transaction failure.")]
    private static partial void LogTransactionFailure(ILogger logger, Exception exception, string command);
}
