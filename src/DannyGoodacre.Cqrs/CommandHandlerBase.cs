using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class CommandHandlerBase<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    internal CommandHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// The display name of the command.
    /// </summary>
    protected abstract string CommandName { get; }

    /// <summary>
    /// The <see cref="ILogger"/> instance for structured reporting.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Validate the command before execution.
    /// </summary>
    /// <param name="validationState">A <see cref="ValidationState"/> to populate with the operation's outcome.</param>
    /// <param name="command">The command to validate.</param>
    protected virtual void Validate(ValidationState validationState, TCommand command) { }

    /// <summary>
    /// The internal command logic.
    /// </summary>
    /// <param name="command">The valid command to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result"/> or <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<TResult> InternalExecuteAsync(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic.
    /// </summary>
    /// <param name="command">The command to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result"/> or <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected async virtual Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationState = new ValidationState();

            Validate(validationState, command);

            if (validationState.HasErrors)
            {
                Logger.LogCommandFailedValidation(CommandName, validationState);

                return Invalid(validationState);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                return await InternalExecuteAsync(command, cancellationToken);
            }

            Logger.LogCommandCanceledBeforeExecution(CommandName);

            return Canceled();

        }
        catch (OperationCanceledException)
        {
            Logger.LogCommandCanceledDuringExecution(CommandName);

            return Canceled();
        }
        catch (Exception ex)
        {
            Logger.LogCommandFailed(CommandName, ex);

            return InternalError(ex.Message);
        }
    }

    protected TResult Invalid(ValidationState validationState) => MapResult(Result.Invalid(validationState));

    protected TResult DomainError(string error) => MapResult(Result.DomainError(error));

    protected TResult Conflict(string error) => MapResult(Result.Conflict(error));

    protected TResult Canceled() => MapResult(Result.Canceled());

    protected TResult NotFound() => MapResult(Result.NotFound());

    protected TResult InternalError(string error) => MapResult(Result.InternalError(error));

    protected TResult InternalError(Exception exception) => MapResult(Result.InternalError(exception));

    protected private abstract TResult MapResult(Result result);
}
