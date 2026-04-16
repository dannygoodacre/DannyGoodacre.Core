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
    : TransactionCommandHandlerBase<TCommand, Result>(logger, transactionUnit)
    where TCommand : ICommand
{
    protected private override Result MapResult(Result result) => result;
}

/// <summary>
/// A standardized workflow for validating and performing actions that persist changes to the
/// application state within a transaction and return a value.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <param name="transactionUnit">The unit of work providing transaction orchestration.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
/// <typeparam name="TResult">The type of the return value in <see cref="Result{T}"/>.</typeparam>
public abstract class TransactionCommandHandler<TCommand, TResult>(ILogger logger, ITransactionUnit transactionUnit)
    : TransactionCommandHandlerBase<TCommand, Result<TResult>>(logger, transactionUnit)
    where TCommand : ICommand
{
    protected private override Result<TResult> MapResult(Result result) => new(result);
}
