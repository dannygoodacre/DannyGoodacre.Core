using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class CommandHandler<TCommand, TResult>(ILogger logger) where TCommand : ICommand
{
    protected abstract string CommandName { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Validate the command before execution.
    /// </summary>
    /// <param name="validationState">A <see cref="ValidationState"/> to populate with the operation's outcome.</param>
    /// <param name="command">The command request to validate.</param>
    protected virtual void Validate(ValidationState validationState, TCommand command)
    {
    }

    /// <summary>
    /// The internal command logic.
    /// </summary>
    /// <param name="command">The valid command request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<Result<TResult>> InternalExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Run the command by validating first and, if successful, execute the internal logic.
    /// </summary>
    /// <param name="command">The command request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected async Task<Result<TResult>> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
    {
        var validationState = new ValidationState();

        Validate(validationState, command);

        if (validationState.HasErrors)
        {
            Logger.LogError("Command '{Command}' failed validation: {ValidationState}", CommandName, validationState);

            return Result<TResult>.Invalid(validationState);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation("Command '{Command}' was cancelled before execution.", CommandName);

            return Result<TResult>.Cancelled();
        }

        try
        {
            return await InternalExecuteAsync(command, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Command '{Command}' was cancelled during execution.", CommandName);

            return Result<TResult>.Cancelled();
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "Command '{Command}' failed with exception: {Exception}", CommandName, e.Message);

            return Result<TResult>.InternalError(e.Message);
        }
    }
}
