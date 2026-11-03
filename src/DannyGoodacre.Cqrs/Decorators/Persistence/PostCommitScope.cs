namespace DannyGoodacre.Cqrs;

/// <summary>
/// Holds the post-commit hooks of child commands until the outermost transaction commits.
/// One instance exists for each service scope.
/// </summary>
internal sealed class PostCommitScope
{
    private List<Func<Task>>? _deferredHooks;

    /// <summary>
    /// Indicates whether a transacted command is running in this scope.
    /// </summary>
    public bool IsInTransaction => _deferredHooks is not null;

    /// <summary>
    /// Starts a transaction attempt with an empty list. An execution strategy retry calls this again.
    /// </summary>
    public void Begin() => _deferredHooks = [];

    /// <summary>
    /// Adds hooks to run after the outermost commit.
    /// </summary>
    public void Defer(Func<Task> runHooks) => _deferredHooks?.Add(runHooks);

    /// <summary>
    /// Ends the transaction and returns the deferred hooks. Run them only if the transaction committed.
    /// </summary>
    public IReadOnlyList<Func<Task>> End()
    {
        IReadOnlyList<Func<Task>> deferredHooks = _deferredHooks ?? [];

        _deferredHooks = null;

        return deferredHooks;
    }
}
