using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public interface ITransactedCommand;

internal sealed class TransactionCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger,
    IPostCommitHook<TCommand>? postCommitHook = null) : ICommandHandler<TCommand>
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
            logger.LogTransactionCanceled(commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogTransactionFaulted(commandName, ex);

            return new InternalError(ex);
        }

        if (result.IsFailure || postCommitHook is null)
        {
            return result;
        }

        try
        {
            await postCommitHook.ExecuteAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogPostCommitHookCanceledMidExecution(commandName);
        }
        catch (Exception ex)
        {
            logger.LogPostCommitFaulted(commandName, ex);

            return new InternalError(ex);
        }

        return result;
    }
}

internal sealed class TransactionCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger,
    IPostCommitHook<TCommand, TResult>? postCommitHook = null) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult: notnull
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
            logger.LogTransactionCanceled(commandName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogTransactionFaulted(commandName, ex);

            return new InternalError<TResult>(ex);
        }

        if (result.IsFailure || postCommitHook is null)
        {
            return result;
        }

        try
        {
            await postCommitHook.AfterCommitAsync(command, result, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogPostCommitHookCanceledMidExecution(commandName);
        }
        catch (Exception ex)
        {
            logger.LogPostCommitFaulted(commandName, ex);

            return new InternalError<TResult>(ex);
        }

        return result;
    }
}
