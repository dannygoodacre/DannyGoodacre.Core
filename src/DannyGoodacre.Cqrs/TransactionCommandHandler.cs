using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// A standardized workflow for validating and performing actions that persist changes to the
/// application state within a transaction.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <param name="transactionUnit">The unit of work providing transaction orchestration.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
public abstract class TransactionCommandHandler<TCommand>(ILogger logger, ITransactionUnit transactionUnit)
    : TransactionCommandHandlerBase<TCommand, IResult>(logger, transactionUnit)
    where TCommand : ICommand
{
    protected override IResult Canceled() => new Canceled();

    protected override IResult Conflict(string message) => new Conflict(message);

    protected override IResult DomainError(string message) => new DomainError(message);

    protected override IResult InternalError(Error error) => new InternalError(error);

    protected override IResult Invalid(ValidationState validationState) => new Invalid(validationState);

    protected override IResult NotFound() => new NotFound();

    protected IResult Success() => new Success();

    /// <summary>
    /// The hook executed after the command succeeds and state changes are saved.
    /// </summary>
    /// <param name="command">The processed command.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    protected virtual Task AfterSaveAsync(TCommand command, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    protected override sealed Task AfterSaveAsync(TCommand command, IResult result, CancellationToken cancellationToken = default)
        => result is Success
            ? AfterSaveAsync(command, cancellationToken)
            : Task.CompletedTask;
}

/// <summary>
/// A standardized workflow for validating and performing actions that persist changes to the
/// application state within a transaction and return a value.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <param name="transactionUnit">The unit of work providing transaction orchestration.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
/// <typeparam name="TResult">The type of the return value in <see cref="IResult{T}"/>.</typeparam>
public abstract class TransactionCommandHandler<TCommand, TResult>(ILogger logger, ITransactionUnit transactionUnit)
    : TransactionCommandHandlerBase<TCommand, IResult<TResult>>(logger, transactionUnit)
    where TCommand : ICommand
{
    protected override IResult<TResult> Canceled() => new Canceled<TResult>();

    protected override IResult<TResult> Conflict(string message) => new Conflict<TResult>(message);

    protected override IResult<TResult> DomainError(string message) => new DomainError<TResult>(message);

    protected override IResult<TResult> InternalError(Error error) => new InternalError<TResult>(error);

    protected override IResult<TResult> Invalid(ValidationState validationState) => new Invalid<TResult>(validationState);

    protected override IResult<TResult> NotFound() => new NotFound<TResult>();

    protected IResult<TResult> Success(TResult value) => new Success<TResult>(value);

    /// <summary>
    /// The hook executed after the command succeeds and state changes are saved.
    /// </summary>
    /// <param name="command">The processed command.</param>
    /// <param name="value">The value produced from the command.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    protected virtual Task AfterSaveAsync(TCommand command, TResult value, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    protected override sealed Task AfterSaveAsync(TCommand command, IResult<TResult> result, CancellationToken cancellationToken = default)
        => result is Success<TResult> success
            ? AfterSaveAsync(command, success.Value, cancellationToken)
            : Task.CompletedTask;
}
