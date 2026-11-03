using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal static class PostCommitHookRunner
{
    /// <summary>
    /// Runs post-commit hooks in order. The data is already committed, so a hook failure does not change
    /// the result of the command. The runner logs the failure and continues with the next hook.
    /// The hooks get <see cref="CancellationToken.None"/>, because a canceled request must not lose a side effect
    /// of committed data.
    /// </summary>
    public static async Task RunAsync<THook>(
        IReadOnlyList<THook> hooks,
        Func<THook, CancellationToken, Task> runHook,
        ILogger logger,
        string commandName)
    {
        foreach (THook hook in hooks)
        {
            try
            {
                await runHook(hook, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                logger.LogPostCommitHookCanceledMidExecution(commandName);
            }
            catch (Exception ex)
            {
                logger.LogPostCommitFaulted(commandName, ex);
            }
        }
    }

    /// <summary>
    /// Runs the hooks now, or defers them to the outermost commit if a transaction is active.
    /// </summary>
    public static Task RunOrDeferAsync<THook>(
        PostCommitScope postCommitScope,
        IReadOnlyList<THook> hooks,
        Func<THook, CancellationToken, Task> runHook,
        ILogger logger,
        string commandName)
    {
        if (hooks.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (!postCommitScope.IsInTransaction)
        {
            return RunAsync(hooks, runHook, logger, commandName);
        }

        postCommitScope.Defer(() => RunAsync(hooks, runHook, logger, commandName));

        return Task.CompletedTask;

    }
}
