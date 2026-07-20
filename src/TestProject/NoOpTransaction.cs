using DannyGoodacre.Cqrs;

namespace TestProject;

public class NoOpTransaction : ITransaction
{
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
