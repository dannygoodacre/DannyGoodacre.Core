using DannyGoodacre.Core.Data;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class UnitOfWorkCommandHandler<TCommand>(ILogger logger, IUnitOfWork unitOfWork)
    : CommandHandler<TCommand>(logger) where TCommand : ICommand
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
        var result = await base.ExecuteAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result;
        }

        try
        {
            var actualChanges = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (ExpectedChanges == -1 || actualChanges == ExpectedChanges)
            {
                return result;
            }

            Logger.LogError("Command '{Command}' made an unexpected number of changes: Expected '{Expected}', Actual '{Actual}'.", CommandName, ExpectedChanges, actualChanges);

            return Result.InternalError("Unexpected number of changes saved.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Command '{Command}' failed while saving changes, with exception: {Exception}", CommandName, ex.Message);

            return Result.InternalError(ex.Message);
        }
    }
}
