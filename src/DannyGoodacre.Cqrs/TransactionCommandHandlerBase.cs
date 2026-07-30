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
    /// This is compared against the result of <see cref="IStateUnit.SaveChangesAsync"/>.
    /// </remarks>
    protected virtual int ExpectedChanges { get; set; } = -1;

    protected async override Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            return await TransactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                TResult result = await base.ExecuteAsync(command, ct);

                if (!result.IsSuccess)
                {
                    return result;
                }

                int actualChanges = await TransactionUnit.SaveChangesAsync(ct);

                if (ExpectedChanges == -1 || actualChanges == ExpectedChanges)
                {
                    return result;
                }

                LogUnexpectedNumberOfChanges(Logger, CommandName, ExpectedChanges, actualChanges);

                return MapResult(Result.InternalError("Attempted to persist an unexpected number of changes."));

            }, cancellationToken);
        }
        catch (Exception ex)
        {
            LogFailed(Logger, ex, CommandName);

            return MapResult(Result.InternalError(ex.Message));
        }
    }

    [LoggerMessage(LogLevel.Error, "Command '{Command}' attempted to persist an unexpected number of changes: Expected '{Expected}', Actual '{Actual}'.")]
    private static partial void LogUnexpectedNumberOfChanges(ILogger logger, string command, int expected, int actual);
}
