# Persistence

How the pipeline connects to a database: the `IStateUnit` and `ITransactionUnit` contracts, the EF Core implementation `DbContextTransaction<TDbContext>`, and EF Core execution strategy retries.

## Contracts

The persistence decorators know nothing about EF Core. They depend on two interfaces.

### IStateUnit

```csharp
public interface IStateUnit
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    bool HasActiveTransaction => false;
}
```

`SaveChangesAsync` writes all pending changes in the scope and returns the number of entries written. The state decorator calls it after a successful `IStateCommand` handler.

`HasActiveTransaction` reports whether a transaction is open on the unit. The pipeline reads it only when post-commit hooks are about to run, to detect a transaction it did not start (event 3003; see [Transactions the pipeline did not start](POST-COMMIT-HOOKS.md#transactions-the-pipeline-did-not-start)). The default returns `false`, so existing implementations still compile; implement it to turn on the detection.

### ITransactionUnit

```csharp
public interface ITransactionUnit : IStateUnit
{
    Task<TResultWrapper> ExecuteInTransactionAsync<TResultWrapper>(
        Func<CancellationToken, Task<TResultWrapper>> operation,
        CancellationToken cancellationToken = default)
        where TResultWrapper : IResult;
}
```

`ExecuteInTransactionAsync` runs `operation` in one transaction. An implementation must:

| Outcome of `operation` | Required action |
| --- | --- |
| Returns a result where `IsSuccess` is `true` | Commit, then return the result. |
| Returns a result where `IsFailure` is `true` | Roll back, then return the result. |
| Throws an exception | Roll back, then rethrow. |

The transaction decorator calls it for each `ITransactedStateCommand`, and inside the operation calls the handler and then `SaveChangesAsync`. The operation is a delegate rather than a value so that the unit can run it again after a transient fault; see [Execution strategy retries](#execution-strategy-retries).

## Registration

For EF Core, [DannyGoodacre.Cqrs.EntityFrameworkCore](../../DannyGoodacre.Cqrs.EntityFrameworkCore/README.md) provides `DbContextTransaction<TDbContext>`, which implements both contracts on one `DbContext`. `AddEntityFrameworkUnits<TDbContext>()` registers one scoped instance as both `IStateUnit` and `ITransactionUnit`. Its README describes how it commits, rolls back, and retries. For any other data store, see [Custom implementation](#custom-implementation).

> **Warning:** If you register the units yourself, register one instance for both interfaces. Child state commands must save on the same `DbContext` that holds the transaction.

> **Note:** The pipeline resolves both interfaces for every state command handler, so register them in any application that sends state commands. Queries and plain `ICommand`s do not need them.

## Nested transactions

An EF Core connection can have only one active transaction. Child commands inside an `ITransactedStateCommand` never start a second one:

1. The outermost `ITransactedStateCommand` calls `ExecuteInTransactionAsync`. No transaction is open, so the unit opens one.
2. A child `IStateCommand` only calls `SaveChangesAsync`; the state decorator never opens a transaction. `IStateUnit` and `ITransactionUnit` resolve to the same scoped `DbContextTransaction<TDbContext>`, so the save runs on the `DbContext` that holds the outer transaction. EF Core writes inside it without committing. The database applies its constraints, and later steps can read the rows.
3. A child `ITransactedStateCommand` calls `ExecuteInTransactionAsync`. The unit sees the open transaction and runs the operation inside it.
4. Only the outermost call commits. If a later step fails, the rollback also removes the rows from earlier child saves.

The example tests in [`OnboardCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/OnboardCustomerPipelineTests.cs) prove this on SQLite. In `HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail`, onboarding calls `SaveChangesAsync` twice, but only one transaction starts and commits. In `HandleAsync_WhenPlanDoesNotExist_RollsBackTheSavedCustomer`, the child command sends `INSERT INTO "Customers"`, and after the rollback the table is empty.

## Execution strategy retries

With `EnableRetryOnFailure`, EF Core uses a retrying execution strategy that repeats an operation after a transient fault. Such a strategy does not allow a transaction that you open directly: `BeginTransactionAsync` outside the strategy throws `InvalidOperationException`. The whole transaction must run inside `IExecutionStrategy.ExecuteAsync`.

That is why `ITransactionUnit` receives the work as a delegate. The unit gets the strategy from `DbContext.Database.CreateExecutionStrategy()` and, inside `strategy.ExecuteAsync`, opens the transaction, runs the operation, and commits. After a transient fault it clears the change tracker, and the strategy runs the whole operation again, including the handler and every child command.

### State commands and retries

A standalone `IStateCommand` does not use `ITransactionUnit` or an execution strategy. A retrying strategy retries its `SaveChangesAsync` call, but the pipeline does not run the handler again, so the handler's decisions are not remade on fresh data. When a handler has to run again on retry, use an `ITransactedStateCommand`. A child state command runs inside the outer execution strategy, so the outer retry replays it. A custom `IStateUnit` gets no retries from the pipeline; it has to retry inside `SaveChangesAsync` itself.

### Handlers that can run more than once

Because the strategy replays the whole operation, the handler of an `ITransactedStateCommand`, and every child it calls, can run more than once for one request. Anything that must happen exactly once belongs in a post-commit hook, which runs once, after the commit.

### Custom implementation

To use a different data store, or change the EF Core behavior, implement `ITransactionUnit` yourself (it extends `IStateUnit`, so one class covers both), using [`DbContextTransaction.cs`](../../DannyGoodacre.Cqrs.EntityFrameworkCore/DbContextTransaction.cs) as the reference. Your implementation must:

1. Run the operation inside an already open transaction rather than start a second one.
2. Otherwise start a transaction, inside the store's execution strategy if it has one.
3. Commit on a success result and roll back on a failure result.
4. On an exception, not commit, clear tracked changes, and rethrow.
5. Implement `HasActiveTransaction`, so the pipeline can detect transactions it did not start.
6. Be registered as one scoped instance for both `IStateUnit` and `ITransactionUnit`, as `AddEntityFrameworkUnits<TDbContext>()` does.
