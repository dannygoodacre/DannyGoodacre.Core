using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal sealed class StateCommandDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger,
    PostCommitScope postCommitScope,
    IEnumerable<IPostCommitHook<TCommand>> postCommitHooks) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private static readonly string CommandName = TypeNameFormatter.Format(typeof(TCommand));

    private readonly IPostCommitHook<TCommand>[] _postCommitHooks = [.. postCommitHooks];

    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedStateCommand)
        {
            return result;
        }

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogSaveChangesCanceled(CommandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogSaveChangesFaulted(CommandName, ex);

            return new InternalError(ex);
        }

        if (_postCommitHooks.Length > 0 && !postCommitScope.IsInTransaction && stateUnit.HasActiveTransaction)
        {
            logger.LogPostCommitHookInUnmanagedTransaction(CommandName);
        }

        await PostCommitHookRunner.RunOrDeferAsync(
            postCommitScope,
            _postCommitHooks,
            (hook, ct) => hook.AfterCommitAsync(command, result, ct),
            logger,
            CommandName);

        return result;
    }
}

internal sealed class StateCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger,
    PostCommitScope postCommitScope,
    IEnumerable<IPostCommitHook<TCommand, TResult>> postCommitHooks) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : notnull
{
    private static readonly string CommandName = TypeNameFormatter.Format(typeof(TCommand));

    private readonly IPostCommitHook<TCommand, TResult>[] _postCommitHooks = [.. postCommitHooks];

    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedStateCommand)
        {
            return result;
        }

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogSaveChangesCanceled(CommandName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogSaveChangesFaulted(CommandName, ex);

            return new InternalError<TResult>(ex);
        }

        if (_postCommitHooks.Length > 0 && !postCommitScope.IsInTransaction && stateUnit.HasActiveTransaction)
        {
            logger.LogPostCommitHookInUnmanagedTransaction(CommandName);
        }

        await PostCommitHookRunner.RunOrDeferAsync(
            postCommitScope,
            _postCommitHooks,
            (hook, ct) => hook.AfterCommitAsync(command, result, ct),
            logger,
            CommandName);

        return result;
    }
}
