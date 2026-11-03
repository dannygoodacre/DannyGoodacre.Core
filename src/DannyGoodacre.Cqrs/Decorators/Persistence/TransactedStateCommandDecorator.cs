using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal sealed class TransactedStateCommandDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger,
    PostCommitScope postCommitScope,
    IEnumerable<IPostCommitHook<TCommand>> postCommitHooks) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly IPostCommitHook<TCommand>[] _postCommitHooks = [.. postCommitHooks];

    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not ITransactedStateCommand)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        string commandName = typeof(TCommand).Name;

        bool isOutermost = !postCommitScope.IsInTransaction;

        IResult result;

        IReadOnlyList<Func<Task>> deferredHooks = [];

        try
        {
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                if (isOutermost)
                {
                    postCommitScope.Begin();
                }

                var innerResult = await inner.HandleAsync(command, ct);

                if (innerResult.IsFailure)
                {
                    return innerResult;
                }

                await transactionUnit.SaveChangesAsync(ct);

                return innerResult;
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogTransactionCanceled(commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogTransactionFaulted(commandName, ex);

            return new InternalError(ex);
        }
        finally
        {
            if (isOutermost)
            {
                deferredHooks = postCommitScope.End();
            }
        }

        if (result.IsFailure)
        {
            return result;
        }

        foreach (Func<Task> runDeferredHooks in deferredHooks)
        {
            await runDeferredHooks();
        }

        await PostCommitHookRunner.RunOrDeferAsync(
            postCommitScope,
            _postCommitHooks,
            (hook, ct) => hook.AfterCommitAsync(command, result, ct),
            logger,
            commandName);

        return result;
    }
}

internal sealed class TransactedStateCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ITransactionUnit transactionUnit,
    ILogger<TCommand> logger,
    PostCommitScope postCommitScope,
    IEnumerable<IPostCommitHook<TCommand, TResult>> postCommitHooks) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : notnull
{
    private readonly IPostCommitHook<TCommand, TResult>[] _postCommitHooks = [.. postCommitHooks];

    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not ITransactedStateCommand)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        string commandName = typeof(TCommand).Name;

        bool isOutermost = !postCommitScope.IsInTransaction;

        IResult<TResult> result;

        IReadOnlyList<Func<Task>> deferredHooks = [];

        try
        {
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                if (isOutermost)
                {
                    postCommitScope.Begin();
                }

                var innerResult = await inner.HandleAsync(command, ct);

                if (innerResult.IsFailure)
                {
                    return innerResult;
                }

                await transactionUnit.SaveChangesAsync(ct);

                return innerResult;
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogTransactionCanceled(commandName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogTransactionFaulted(commandName, ex);

            return new InternalError<TResult>(ex);
        }
        finally
        {
            if (isOutermost)
            {
                deferredHooks = postCommitScope.End();
            }
        }

        if (result.IsFailure)
        {
            return result;
        }

        foreach (Func<Task> runDeferredHooks in deferredHooks)
        {
            await runDeferredHooks();
        }

        await PostCommitHookRunner.RunOrDeferAsync(
            postCommitScope,
            _postCommitHooks,
            (hook, ct) => hook.AfterCommitAsync(command, result, ct),
            logger,
            commandName);

        return result;
    }
}
