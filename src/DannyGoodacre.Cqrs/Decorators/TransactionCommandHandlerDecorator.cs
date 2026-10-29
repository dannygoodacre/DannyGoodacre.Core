using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public interface ITransactedCommand;

public sealed class TransactionCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger)
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

        return result;
    }
}

public sealed class TransactionCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult: IResult
{
    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not ITransactedCommand)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        string commandName = typeof(TCommand).Name;

        IResult<TResult> result;

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

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogCommandFailedWhilePersistingChanges(commandName, ex);

            return new InternalError<TResult>(ex);
        }

        // if (result.IsFailure || postCommitHandler is null)
        // {
        //     return result;
        // }
        //
        // try
        // {
        //     await postCommitHandler.AfterSaveAsync(command, result, cancellationToken);
        //
        //     return result;
        // }
        // catch (OperationCanceledException)
        // {
        //     logger.LogCommandCanceledDuringAfterSave(commandName);
        //
        //     return new Canceled();
        // }
        // catch (Exception ex)
        // {
        //     logger.LogCommandFailedDuringAfterSave(commandName, ex);
        //
        //     return new InternalError(ex);
        // }

        return result;
    }
}
