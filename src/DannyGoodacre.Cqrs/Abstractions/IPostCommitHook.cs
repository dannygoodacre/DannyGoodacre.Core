using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

public interface IPostCommitHandler<in TCommand>
    where TCommand : ICommand
{
    Task AfterSaveAsync(TCommand command, IResult result, CancellationToken cancellationToken = default);
}

public interface IPostCommitHook<in TCommand, in TResult>
    where TCommand : ICommand
{
    Task AfterSaveAsync(TCommand command, IResult<TResult> result, CancellationToken cancellationToken = default);
}
