namespace DannyGoodacre.Cqrs;

public sealed class NoOpTransaction : ITransaction
{

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
