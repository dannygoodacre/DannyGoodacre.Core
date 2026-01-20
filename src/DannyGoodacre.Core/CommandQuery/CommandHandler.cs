using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class CommandHandler<TCommandRequest>(ILogger logger) where TCommandRequest : ICommandRequest
{
    protected abstract string CommandName { get; }

    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Validate the command before execution.
    /// </summary>
    /// <param name="validationState">A <see cref="ValidationState"/> to populate with the operation's outcome.</param>
    /// <param name="command">The command request to validate.</param>
    protected virtual void Validate(ValidationState validationState, TCommandRequest command)
    {
    }

    /// <summary>
    /// The internal command logic.
    /// </summary>
    /// <param name="command">The valid command request to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    protected abstract Task<Result> InternalExecuteAsync(TCommandRequest command, CancellationToken cancellationToken);

    /// <summary>
    /// Run the command by validating first and, if valid, execute the internal logic.
    /// </summary>
    /// <param name="commandRequest">The command request to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    protected async virtual Task<Result> ExecuteAsync(TCommandRequest commandRequest, CancellationToken cancellationToken)
    {
        var validationState = new ValidationState();

        Validate(validationState, commandRequest);

        if (validationState.HasErrors)
        {
            Logger.LogError("Command '{Command}' failed validation: {ValidationState}", CommandName, validationState);

            return Result.Invalid(validationState);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation("Command '{Command}' was cancelled before execution.", CommandName);

            return Result.Cancelled();
        }

        try
        {
            return await InternalExecuteAsync(commandRequest, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Command '{Command}' was cancelled during execution.", CommandName);

            return Result.Cancelled();
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "Command '{Command}' failed with exception: {Exception}", CommandName, e.Message);

            return Result.InternalError(e.Message);
        }
    }
}
