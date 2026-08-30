using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// A standardized workflow for validating and performing actions that persist changes to the
/// application state.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <param name="stateUnit">The state unit for persisting changes.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
public abstract class StateCommandHandler<TCommand>(ILogger logger, IStateUnit stateUnit)
    : StateCommandHandlerBase<TCommand, IResult>(logger, stateUnit)
    where TCommand : ICommand
{
    /// <summary>
    /// The internal command logic.
    /// </summary>
    /// <param name="command">The valid command to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<IResult> InternalExecuteAsync(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic.
    /// </summary>
    /// <param name="command">The command to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{T}"/> indicating the outcome of the operation.</returns>
    protected Task<IResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
        => BaseExecuteAsync(command,
                            InternalExecuteAsync,
                            VoidResultFactories.OnInvalid,
                            VoidResultFactories.OnCanceled,
                            VoidResultFactories.OnInternalError,
                            cancellationToken);

    protected IResult Success() => new Success();
}

/// <summary>
/// A standardized workflow for validating and performing actions that persist changes to the
/// application state and return a value.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <param name="stateUnit">The state unit for persisting changes.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
/// <typeparam name="TResult">The type of the return value in <see cref="IResult{T}"/>.</typeparam>
public abstract class StateCommandHandler<TCommand, TResult>(ILogger logger, IStateUnit stateUnit)
    : StateCommandHandlerBase<TCommand, IResult<TResult>>(logger, stateUnit)
    where TCommand : ICommand
{
    /// <summary>
    /// The internal command logic.
    /// </summary>
    /// <param name="command">The valid command to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<IResult> InternalExecuteAsync(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the command by validating it first and, if valid, execute the internal logic.
    /// </summary>
    /// <param name="command">The command to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{T}"/> indicating the outcome of the operation.</returns>
    protected Task<IResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
        => BaseExecuteAsync(command,
                            InternalExecuteAsync,
                            _onInvalid,
                            _onCanceled,
                            _onInternalError,
                            cancellationToken);

    protected IResult<TResult> Success(TResult result) => new Success<TResult>(result);

    private readonly static Func<ValidationState, Invalid<TResult>> _onInvalid = validationState => new Invalid<TResult>(validationState);

    private readonly static Func<Canceled<TResult>> _onCanceled = () => new Canceled<TResult>();

    private readonly static Func<Error, InternalError<TResult>> _onInternalError = error => new InternalError<TResult>(error);
}
