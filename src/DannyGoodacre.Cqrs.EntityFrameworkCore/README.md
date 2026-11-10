# DannyGoodacre.Cqrs.EntityFrameworkCore

Entity Framework Core persistence for [DannyGoodacre.Cqrs](../DannyGoodacre.Cqrs/README.md). It provides `DbContextTransaction<TDbContext>`, which implements the pipeline's `IStateUnit` and `ITransactionUnit` on one `DbContext`, so state commands save and run in transactions through EF Core.

## Installation

```shell
dotnet add package DannyGoodacre.Cqrs.EntityFrameworkCore
```

> **Note:** The package is currently published to GitHub Packages only; publication to nuget.org is planned. See the [installation note](../DannyGoodacre.Cqrs/README.md#installation) for adding the feed.

## Registration

Register your `DbContext` as usual, then call `AddEntityFrameworkUnits<TDbContext>()`:

```csharp
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    connectionString,
    sql => sql.EnableRetryOnFailure()));

services.AddEntityFrameworkUnits<AppDbContext>();

services.AddCqrsHandlers(typeof(Program).Assembly);
```

`AddEntityFrameworkUnits<TDbContext>()` registers one scoped `DbContextTransaction<TDbContext>` and resolves both `IStateUnit` and `ITransactionUnit` to it. Sharing one instance is what lets child state commands save inside the outer command's transaction. The units use whichever provider and execution strategy you configure on the `DbContext`.

The units are registered for a single `DbContext` type. Calling `AddEntityFrameworkUnits` for a second type replaces the first.

## Behavior

- `SaveChangesAsync` calls `DbContext.SaveChangesAsync`.
- `HasActiveTransaction` is `true` when `DbContext.Database.CurrentTransaction` is not `null`.
- `ExecuteInTransactionAsync` gets the execution strategy from `DbContext.Database.CreateExecutionStrategy()` and, inside it, opens a transaction, runs the operation, and commits on success. With a retrying strategy, a transient fault replays the whole operation. See [Execution strategy retries](../DannyGoodacre.Cqrs/docs/PERSISTENCE.md#execution-strategy-retries).
- If a transaction is already open on the `DbContext`, `ExecuteInTransactionAsync` runs the operation inside it rather than starting a second one. See [Nested transactions](../DannyGoodacre.Cqrs/docs/PERSISTENCE.md#nested-transactions).

| Event | `DbContextTransaction<TDbContext>` | Pipeline result |
| --- | --- | --- |
| Handler returns a failure | Rolls back and clears the change tracker. | The handler result |
| Handler or `SaveChangesAsync` throws | Does not commit, clears the change tracker, rethrows. | `InternalError` |
| The operation is canceled | Does not commit, clears the change tracker, rethrows. | `Canceled` |
| All steps succeed | Commits. | The handler result |

Clearing the change tracker discards the entities from the failed steps, so a later use of the same `DbContext` in the scope cannot save them by accident.

For a data store other than EF Core, implement the two interfaces yourself, using [`DbContextTransaction.cs`](DbContextTransaction.cs) as the reference. See [Custom implementation](../DannyGoodacre.Cqrs/docs/PERSISTENCE.md#custom-implementation).
