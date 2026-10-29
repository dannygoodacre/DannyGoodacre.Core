using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

public interface IPostCommitHandler<in TCommand, in TResult>
    where TCommand : ICommand
    where TResult : IResult
{
    Task AfterSaveAsync(TCommand command, IResult result, CancellationToken cancellationToken);
}
