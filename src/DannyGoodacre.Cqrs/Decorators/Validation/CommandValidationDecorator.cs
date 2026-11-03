using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal sealed class CommandValidationDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<TCommand> logger,
    IEnumerable<IValidator<TCommand>> validators)
    : ValidationDecoratorBase<TCommand>(logger, validators), ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        => ExecuteWithValidationAsync(command, ct => inner.HandleAsync(command, ct), cancellationToken);
}

internal sealed class CommandValidationDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<TCommand> logger,
    IEnumerable<IValidator<TCommand>> validators)
    : ValidationDecoratorBase<TCommand, TResult>(logger, validators), ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : notnull
{
    public Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        => ExecuteWithValidationAsync(command, ct => inner.HandleAsync(command, ct), cancellationToken);
}
