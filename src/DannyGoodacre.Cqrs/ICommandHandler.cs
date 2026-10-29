using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand
{
    Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
