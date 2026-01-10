using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class TransactionCommandHandler<TCommand>(ILogger logger, IUnitOfWork unitOfWork)
    : CommandHandler<TCommand>(logger) where TCommand : ICommandRequest
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

    /// <summary>
    /// Run the command by validating first and, if valid, execute the internal logic.
    /// If the command executes successfully, save the changes to the database.
    /// </summary>
    /// <param name="command">The command request to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the outcome of the operation.
    /// </returns>
    protected async override Task<Result> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await base.ExecuteAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return result;
            }

            var actualChanges = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (ExpectedChanges != -1 && actualChanges != ExpectedChanges)
            {
                await transaction.RollbackAsync(cancellationToken);

                Logger.LogError("Command '{Command}' attempted to persist an unexpected number of changes: Expected '{Expected}', Actual '{Actual}'.", CommandName, ExpectedChanges, actualChanges);

                return Result.InternalError("Database integrity check failed.");
            }

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Logger.LogCritical("Command '{Command}' experienced a transaction failure: {Exception}", CommandName, ex.Message);

            return Result.InternalError(ex.Message);
        }
    }
}
