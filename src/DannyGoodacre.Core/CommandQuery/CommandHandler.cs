using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

/// <summary>
/// A standardized workflow for validating and performing actions with side effects, without
/// persisting changes to the application state.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
public abstract class CommandHandler<TCommand>(ILogger logger)
    : CommandHandlerBase<TCommand, Result>(logger)
    where TCommand : ICommand
{
    protected private override Result MapResult(Result result) => result;
}

/// <summary>
/// A standardized workflow for validating and performing actions with side effects, which return
/// a value without persisting changes to the application state.
/// </summary>
/// <param name="logger">The logger used for structured reporting.</param>
/// <typeparam name="TCommand">The type of <see cref="ICommand"/> to be handled.</typeparam>
/// <typeparam name="TResult">The type of the return value in <see cref="Result{T}"/>.</typeparam>
public abstract class CommandHandler<TCommand, TResult>(ILogger logger)
    : CommandHandlerBase<TCommand, Result<TResult>>(logger)
    where TCommand : ICommand
{
    protected private override Result<TResult> MapResult(Result result) => new(result);
}
