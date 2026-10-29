using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public interface IStateCommand;

public sealed class StateCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedCommand)
        {
            return result;
        }

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledWhilePersistingChanges(typeof(TCommand).Name);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedWhilePersistingChanges(typeof(TCommand).Name, ex);

            return new InternalError(ex);
        }
    }
}

public sealed class StateCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : IResult
{
    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedCommand)
        {
            return result;
        }

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledWhilePersistingChanges(typeof(TCommand).Name);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedWhilePersistingChanges(typeof(TCommand).Name, ex);

            return new InternalError<TResult>(ex);
        }
    }
}
