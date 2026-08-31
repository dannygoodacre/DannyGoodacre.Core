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
    protected override IResult Canceled() => new Canceled();

    protected override IResult Conflict(string error) => new Conflict(error);

    protected override IResult DomainError(string error) => new DomainError(error);

    protected override IResult InternalError(Error error) => new InternalError(error);

    protected override IResult Invalid(ValidationState validationState) => new Invalid(validationState);

    protected override IResult NotFound() => new NotFound();

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
    protected override IResult<TResult> Canceled() => new Canceled<TResult>();

    protected override IResult<TResult> Conflict(string error) => new Conflict<TResult>(error);

    protected override IResult<TResult> DomainError(string error) => new DomainError<TResult>(error);

    protected override IResult<TResult> InternalError(Error error) => new InternalError<TResult>(error);

    protected override IResult<TResult> Invalid(ValidationState validationState) => new Invalid<TResult>(validationState);

    protected override IResult<TResult> NotFound() => new NotFound<TResult>();

    protected IResult<TResult> Success(TResult result) => new Success<TResult>(result);
}
