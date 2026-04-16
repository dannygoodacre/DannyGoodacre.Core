using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract partial class CommandHandlerBase<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    internal CommandHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    protected ILogger Logger { get; }

    /// <summary>
    /// The display name of the command.
    /// </summary>
    protected abstract string CommandName { get; }

    /// <summary>
    /// Validate the command before execution.
    /// </summary>
    /// <param name="validationState">A <see cref="ValidationState"/> to populate with the operation's outcome.</param>
    /// <param name="command">The command request to validate.</param>
    protected virtual void Validate(ValidationState validationState, TCommand command) { }

    /// <summary>
    /// The core command logic.
    /// </summary>
    /// <param name="command">The valid command to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the outcome of the operation.
    /// </returns>
    protected abstract Task<TResult> InternalExecuteAsync(TCommand command, CancellationToken cancellationToken = default);

    protected private abstract TResult MapResult(Result result);

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic.
    /// </summary>
    /// <param name="command">The command to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    // ReSharper disable once MemberCanBeProtected.Global
    public async virtual Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var validationState = new ValidationState();

        Validate(validationState, command);

        if (validationState.HasErrors)
        {
            LogFailedValidation(Logger, CommandName, validationState);

            return MapResult(Result.Invalid(validationState));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            LogCanceledBeforeExecution(Logger, CommandName);

            return MapResult(Result.Canceled());
        }

        try
        {
            return await InternalExecuteAsync(command, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogCanceledDuringExecution(Logger, CommandName);

            return MapResult(Result.Canceled());
        }
        catch (Exception ex)
        {
            LogFailed(Logger, ex, CommandName);

            return MapResult(Result.InternalError(ex.Message));
        }
    }

    [LoggerMessage(LogLevel.Error, "Command '{Command}' failed validation: {ValidationState}")]
    private static partial void LogFailedValidation(ILogger logger, string command, ValidationState validationState);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled before execution.")]
    private static partial void LogCanceledBeforeExecution(ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled during execution.")]
    private static partial void LogCanceledDuringExecution(ILogger logger, string command);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception, string command);
}
