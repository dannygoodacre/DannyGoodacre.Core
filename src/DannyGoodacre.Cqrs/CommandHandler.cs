using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// A standardized workflow for validating and performing actions with side effects, without
/// persisting changes to the application state.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
public abstract class CommandHandler<TCommand>(ILogger logger)
    : CommandHandlerBase<TCommand, IResult>(logger)
    where TCommand : ICommand
{
    protected override IResult Canceled() => new Canceled();

    protected override IResult Conflict(string message) => new Conflict(message);

    protected override IResult DomainError(string message) => new DomainError(message);

    protected override IResult InternalError(Error error) => new InternalError(error);

    protected override IResult Invalid(ValidationState validationState) => new Invalid(validationState);

    protected override IResult NotFound() => new NotFound();

    protected IResult Success() => new Success();
}

/// <summary>
/// A standardized workflow for validating and performing actions with side effects, which return
/// a value without persisting changes to the application state.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
/// <typeparam name="TResultType">The type of the return value in <see cref="IResult{T}"/>.</typeparam>
public abstract class CommandHandler<TCommand, TResultType>(ILogger logger)
    : CommandHandlerBase<TCommand, IResult<TResultType>>(logger)
    where TCommand : ICommand
{
    protected override IResult<TResultType> Canceled() => new Canceled<TResultType>();

    protected override IResult<TResultType> Conflict(string message) => new Conflict<TResultType>(message);

    protected override IResult<TResultType> DomainError(string message) => new DomainError<TResultType>(message);

    protected override IResult<TResultType> InternalError(Error error) => new InternalError<TResultType>(error);

    protected override IResult<TResultType> Invalid(ValidationState validationState) => new Invalid<TResultType>(validationState);

    protected override IResult<TResultType> NotFound() => new NotFound<TResultType>();

    protected IResult<TResultType> Success(TResultType value) => new Success<TResultType>(value);
}
