using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal sealed class CommandLoggingDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<TCommand> logger)
    : LoggingDecoratorBase<TCommand>(logger), ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        => ExecuteWithLoggingAsync(command, ct => inner.HandleAsync(command, ct), cancellationToken);
}

internal sealed class CommandLoggingDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<TCommand> logger)
    : LoggingDecoratorBase<TCommand, TResponse>(logger), ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
    where TResponse : notnull
{
    public Task<IResult<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        => ExecuteWithLoggingAsync(command, ct => inner.HandleAsync(command, ct), cancellationToken);
}
