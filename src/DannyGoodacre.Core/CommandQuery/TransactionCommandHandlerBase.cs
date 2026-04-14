using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract partial class TransactionCommandHandlerBase<TCommand, TResult>
    : CommandHandlerBase<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    internal TransactionCommandHandlerBase(ILogger logger, ITransactionUnit transactionUnit)
        : base(logger)
    {
        TransactionUnit = transactionUnit;
    }

    private ITransactionUnit TransactionUnit { get; }

    /// <summary>
    /// The number of state entries expected to be persisted upon completion.
    /// </summary>
    /// <value>
    /// Defaults to -1 to disable validation.
    /// </value>
    /// <remarks>
    /// This is compared against the result of <see cref="IStateUnit.SaveChangesAsync"/>.
    /// </remarks>
    protected virtual int ExpectedChanges { get; set; } = -1;

    public async override Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        await using ITransaction transaction = await TransactionUnit.BeginTransactionAsync(cancellationToken);

        try
        {
            TResult result = await base.ExecuteAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return result;
            }

            int actualChanges = await TransactionUnit.SaveChangesAsync(cancellationToken);

            if (ExpectedChanges != -1 && actualChanges != ExpectedChanges)
            {
                await transaction.RollbackAsync(cancellationToken);

                LogUnexpectedNumberOfChanges(Logger, CommandName, ExpectedChanges, actualChanges);

                return MapResult(Result.InternalError("State integrity check failed."));
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
