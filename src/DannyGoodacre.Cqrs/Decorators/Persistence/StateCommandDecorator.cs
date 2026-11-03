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
    private readonly IPostCommitHook<TCommand>[] _postCommitHooks = [.. postCommitHooks];

    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedStateCommand)
        {
            return result;
        }

        string commandName = typeof(TCommand).Name;

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogSaveChangesCanceled(commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogSaveChangesFaulted(commandName, ex);

            return new InternalError(ex);
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

internal sealed class StateCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IStateUnit stateUnit,
    ILogger<TCommand> logger,
    PostCommitScope postCommitScope,
    IEnumerable<IPostCommitHook<TCommand, TResult>> postCommitHooks) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : notnull
{
    private readonly IPostCommitHook<TCommand, TResult>[] _postCommitHooks = [.. postCommitHooks];

    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure || command is not IStateCommand || command is ITransactedStateCommand)
        {
            return result;
        }

        string commandName = typeof(TCommand).Name;

        try
        {
            await stateUnit.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogSaveChangesCanceled(commandName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogSaveChangesFaulted(commandName, ex);

            return new InternalError<TResult>(ex);
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
