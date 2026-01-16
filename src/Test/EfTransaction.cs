using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Test;

internal sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction, IDisposable
{
    private bool _isDisposed;

    public Task CommitAsync(CancellationToken cancellationToken)
        => transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken)
        => transaction.RollbackAsync(cancellationToken);

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        transaction.Dispose();

        _isDisposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await transaction.DisposeAsync();

        _isDisposed = true;
    }
}
