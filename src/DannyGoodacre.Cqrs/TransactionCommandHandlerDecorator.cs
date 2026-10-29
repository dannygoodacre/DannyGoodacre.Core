using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public interface ITransactedCommand;

public sealed class TransactionCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger,
    IPostCommitHandler<TCommand, IResult>? postCommitHandler = null)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<IResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is not ITransactedCommand)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        string commandName = typeof(TCommand).Name;
        IResult result;

        try
        {
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                var innerResult = await inner.HandleAsync(command, ct);

                if (innerResult.IsFailure)
                {
                    return innerResult;
                }

                await transactionUnit.SaveChangesAsync(ct);

                return innerResult;

            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledWhilePersistingChanges(commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedWhilePersistingChanges(commandName, ex);

            return new InternalError(ex);
        }

        if (result.IsFailure || postCommitHandler is null)
        {
            return result;
        }

        try
        {
            await postCommitHandler.AfterSaveAsync(command, result, cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogCommandCanceledDuringAfterSave(commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedDuringAfterSave(commandName, ex);

            return new InternalError(ex);
        }
    }
}
