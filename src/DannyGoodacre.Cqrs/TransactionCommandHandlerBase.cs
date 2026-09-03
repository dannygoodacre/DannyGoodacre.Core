using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class TransactionCommandHandlerBase<TCommand, TResult> : CommandHandlerBase<TCommand, TResult>
    where TCommand : ICommand
    where TResult : IResult
{
    internal TransactionCommandHandlerBase(ILogger logger, ITransactionUnit transactionUnit) : base(logger)
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
    /// This is compared against the result of <see cref="IStateUnit.SaveChangesAsync"/>.
    /// </remarks>
    protected virtual int ExpectedChanges { get; set; } = -1;

    protected virtual Task AfterSaveAsync(TCommand command, TResult result, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic in a transaction.
    /// If the command succeeds, persist all state changes and call <see cref="AfterSaveAsync"/>.
    /// </summary>
    /// <inheritdoc cref="CommandHandlerBase{TCommand,TResult}.ExecuteAsync" />
    protected new async Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        TResult result;

        try
        {
            result = await TransactionUnit.ExecuteInTransactionAsync(async innerCancellationToken =>
            {
                TResult innerResult = await base.ExecuteAsync(command, innerCancellationToken);

                if (innerResult is not Success)
                {
                    return innerResult;
                }

                int actualChanges = await TransactionUnit.SaveChangesAsync(innerCancellationToken);

                if (ExpectedChanges == -1 || actualChanges == ExpectedChanges)
                {
                    return innerResult;
                }

                Logger.LogCommandUnexpectedNumberOfChanges(CommandName, ExpectedChanges, actualChanges);

                return InternalError("Attempted to persist an unexpected number of changes.");

            }, cancellationToken);
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
