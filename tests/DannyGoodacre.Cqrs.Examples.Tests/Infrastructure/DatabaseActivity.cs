using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;

/// <summary>
/// An EF Core interceptor that records each SQL command and each transaction event.
/// </summary>
internal sealed class DatabaseActivity : IDbCommandInterceptor, IDbTransactionInterceptor
{
    private readonly ConcurrentQueue<string> _commands = new();

    private int _transactionsStarted;

    private int _transactionsCommitted;

    private int _transactionsRolledBack;

    public IReadOnlyCollection<string> Commands => _commands.ToArray();

    public int TransactionsStarted => Volatile.Read(ref _transactionsStarted);

    public int TransactionsCommitted => Volatile.Read(ref _transactionsCommitted);

    public int TransactionsRolledBack => Volatile.Read(ref _transactionsRolledBack);

    public void Reset()
    {
        _commands.Clear();
        Interlocked.Exchange(ref _transactionsStarted, 0);
        Interlocked.Exchange(ref _transactionsCommitted, 0);
        Interlocked.Exchange(ref _transactionsRolledBack, 0);
    }

    public InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        => Record(command, result);

    public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Record(command, result));

    public InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        => Record(command, result);

    public ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Record(command, result));

    public InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        => Record(command, result);

    public ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Record(command, result));

    public DbTransaction TransactionStarted(
        DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
    {
        Interlocked.Increment(ref _transactionsStarted);

        return result;
    }

    public ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection, TransactionEndEventData eventData, DbTransaction result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(TransactionStarted(connection, eventData, result));

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        => Interlocked.Increment(ref _transactionsCommitted);

    public Task TransactionCommittedAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        TransactionCommitted(transaction, eventData);

        return Task.CompletedTask;
    }

    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        => Interlocked.Increment(ref _transactionsRolledBack);

    public Task TransactionRolledBackAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        TransactionRolledBack(transaction, eventData);

        return Task.CompletedTask;
    }

    private T Record<T>(DbCommand command, T result)
    {
        _commands.Enqueue(command.CommandText);

        return result;
    }
}
