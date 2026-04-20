using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

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

    /// <summary>
    /// The transaction provider for the lifecycle of this command.
    /// </summary>
    /// <remarks>
    /// This is used in derived classes to perform additional data persistence and transaction control.
    /// </remarks>
    // ReSharper disable once MemberCanBePrivate.Global
    protected ITransactionUnit TransactionUnit { get; }

    /// <summary>
    /// The number of state entries expected to be persisted upon completion of the command.
    /// </summary>
    /// <value>
    /// Defaults to -1 to disable validation.
    /// </value>
    /// <remarks>
    /// This is compared against the result of <see cref="ITransactionUnit.SaveChangesAsync"/>.
    /// </remarks>
    protected virtual int ExpectedChanges { get; set; } = -1;

    protected async override Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
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

                return MapResult(Result.InternalError("Attempted to persist an unexpected number of changes."));
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
