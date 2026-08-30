using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class CommandHandlerBase<TCommand>
    where TCommand : ICommand
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

    protected private async Task<TResult> BaseExecuteAsync<TResult>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<TResult>> internalExecuteAsync,
        Func<ValidationState, TResult> onInvalid,
        Func<TResult> onCanceled,
        Func<Error, TResult> onInternalError,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationState = new ValidationState();

            Validate(validationState, command);

            if (validationState.HasErrors)
            {
                Logger.LogCommandFailedValidation(CommandName, validationState);

                return onInvalid(validationState);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                return await internalExecuteAsync(command, cancellationToken);
            }

            Logger.LogCommandCanceledBeforeExecution(CommandName);

            return onCanceled();

        }
        catch (OperationCanceledException)
        {
            Logger.LogCommandCanceledDuringExecution(CommandName);

            return onCanceled();
        }
        catch (Exception ex)
        {
            Logger.LogCommandFailed(CommandName, ex);

            return onInternalError(ex.Message);
        }
    }

    protected IResult Invalid(ValidationState validationState) => Result.Invalid(validationState);

    protected IResult DomainError(string error) => Result.DomainError(error);

    protected IResult Conflict(string error) => Result.Conflict(error);

    protected IResult Canceled() => Result.Canceled();

    protected IResult NotFound() => Result.NotFound();

    protected IResult InternalError(string error) => Result.InternalError(error);

    protected IResult InternalError(Exception exception) => Result.InternalError(exception);
}
