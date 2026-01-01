using DannyGoodacre.Core.Data;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class UnitOfWorkCommandHandler<TCommand, TResult>(ILogger logger, IUnitOfWork unitOfWork)
    : CommandHandler<TCommand, TResult>(logger) where TCommand : ICommand
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
    /// <param name="command">The command request to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    /// </returns>
    protected async override Task<Result<TResult>> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
    {
        var result = await base.ExecuteAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var actualChanges = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (ExpectedChanges != -1 && actualChanges != ExpectedChanges)
            {
                Logger.LogError("Command '{Command}' made an unexpected number of changes to the database: Expected '{Expected}', actual '{Actual}'.", CommandName, ExpectedChanges, actualChanges);
            }
        }

        return result;
    }
}
