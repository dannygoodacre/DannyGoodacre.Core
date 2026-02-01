using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class PersistenceCommandHandler<TCommand>(ILogger logger, IUnitOfWork unitOfWork)
    : PersistenceCommandHandlerBase<TCommand, Result>(logger, unitOfWork)
    where TCommand : ICommand
{
    protected private override Result MapResult(Result result)
        => result;
}

public abstract class PersistenceCommandHandler<TCommand, TResult>(ILogger logger, IUnitOfWork unitOfWork)
    : PersistenceCommandHandlerBase<TCommand, Result<TResult>>(logger, unitOfWork)
    where TCommand : ICommand
{
    protected private override Result<TResult> MapResult(Result result)
        => new(result);
}
