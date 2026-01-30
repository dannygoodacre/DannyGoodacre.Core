using DannyGoodacre.Core.CommandQuery.Abstractions;

namespace DannyGoodacre.Identity.Tests.Harness;

public sealed class TestUnitOfWork(TestIdentityContext identityContext) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private bool _isDisposed;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => identityContext.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => new EfTransaction(await identityContext.Database.BeginTransactionAsync(cancellationToken));

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        identityContext.Dispose();

        _isDisposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await identityContext.DisposeAsync();

        _isDisposed = true;
    }
}
