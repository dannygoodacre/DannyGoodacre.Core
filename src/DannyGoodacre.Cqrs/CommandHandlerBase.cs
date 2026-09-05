using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class CommandHandlerBase<TCommand, TResultWrapper>
    where TCommand : ICommand
    where TResultWrapper : IResult
{
    internal CommandHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    protected abstract string CommandName { get; }

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
    /// <returns>An <see cref="IResult"/> indicating the outcome of the operation.</returns>
    protected abstract Task<TResultWrapper> InternalExecuteAsync(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic.
    /// </summary>
    /// <param name="command">The command to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult"/> indicating the outcome of the operation.</returns>
    protected async Task<TResultWrapper> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
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

    protected abstract TResultWrapper Canceled();

    protected abstract TResultWrapper Conflict(string message);

    protected abstract TResultWrapper DomainError(string message);

    protected abstract TResultWrapper InternalError(Error error);

    protected abstract TResultWrapper Invalid(ValidationState validationState);

    protected abstract TResultWrapper NotFound();
}
