using DannyGoodacre.Core.CommandQuery.Abstractions;

namespace Test;

internal sealed class UnitOfWork(ApplicationContext context) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private bool _isDisposed;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => new EfTransaction(await context.Database.BeginTransactionAsync(cancellationToken));

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        context.Dispose();

        _isDisposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await context.DisposeAsync();

        _isDisposed = true;
    }
}
