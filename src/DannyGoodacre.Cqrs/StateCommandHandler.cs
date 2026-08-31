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
/// <typeparam name="TResultType">The type of the return value in <see cref="IResult{T}"/>.</typeparam>
public abstract class StateCommandHandler<TCommand, TResultType>(ILogger logger, IStateUnit stateUnit)
    : StateCommandHandlerBase<TCommand, IResult<TResultType>>(logger, stateUnit)
    where TCommand : ICommand
{
    protected override IResult<TResultType> Canceled() => new Canceled<TResultType>();

    protected override IResult<TResultType> Conflict(string error) => new Conflict<TResultType>(error);

    protected override IResult<TResultType> DomainError(string error) => new DomainError<TResultType>(error);

    protected override IResult<TResultType> InternalError(Error error) => new InternalError<TResultType>(error);

    protected override IResult<TResultType> Invalid(ValidationState validationState) => new Invalid<TResultType>(validationState);

    protected override IResult<TResultType> NotFound() => new NotFound<TResultType>();

    protected IResult<TResultType> Success(TResultType result) => new Success<TResultType>(result);
}
