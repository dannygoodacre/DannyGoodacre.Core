using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines post-commit side effect logic executed strictly after changes from a void <see cref="ICommand"/> have successfully persisted.
/// </summary>
/// <typeparam name="TCommand">The type of command that was executed.</typeparam>
public interface IPostCommitHook<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Executes side effects after the primary database transaction or unit of work has committed.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="result">The outcome returned by the command handler.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task AfterSaveAsync(TCommand command, IResult result, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines post-commit side effect logic executed strictly after changes from a value-returning <see cref="ICommand"/> have successfully persisted.
/// </summary>
/// <typeparam name="TCommand">The type of command that was executed.</typeparam>
/// <typeparam name="TResult">The return payload type produced by the command execution.</typeparam>
public interface IPostCommitHook<in TCommand, in TResult>
    where TCommand : ICommand
{
    /// <summary>
    /// Executes side effects after the primary database transaction or unit of work has committed.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="result">The result payload returned by the command handler.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task AfterSaveAsync(TCommand command, IResult<TResult> result, CancellationToken cancellationToken = default);
}
