# Persistence

This document tells you how to connect the pipeline to a database.
It describes `IStateUnit`, `ITransactionUnit`, and `DbContextTransaction<TDbContext>` for Entity Framework Core.
It also describes EF Core execution strategy retries.

## Contracts

The persistence decorators do not know about EF Core. They use two interfaces.

### IStateUnit

```csharp
public interface IStateUnit
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    bool HasActiveTransaction => false;
}
```

`SaveChangesAsync` writes all pending changes in the current scope to the data store.
It returns the number of entries that it wrote.

The state decorator calls `SaveChangesAsync` after a successful `IStateCommand` handler.

`HasActiveTransaction` shows whether a transaction is open on the unit. The pipeline reads it only when post-commit hooks are about to run.
If a transaction is open that the pipeline did not start, the pipeline logs event 3003. See [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md#scope-and-transaction-rules).
The default implementation returns `false`, so an existing implementation still compiles. Implement the member to turn on the detection.

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

`ExecuteInTransactionAsync` runs `operation` inside one transaction. An implementation must obey these rules:

| Outcome of `operation` | Required action |
| --- | --- |
| Returns a result where `IsSuccess` is `true` | Commit the transaction. Return the result. |
| Returns a result where `IsFailure` is `true` | Roll back the transaction. Return the result. |
| Throws an exception | Roll back the transaction. Throw the exception again. |

The transaction decorator calls `ExecuteInTransactionAsync` for each `ITransactedStateCommand`.
Inside the operation, it calls the handler and then `SaveChangesAsync`.

The operation is a delegate, not a value. Thus, the unit can run the operation again after a transient fault.
See [Execution strategy retries](#execution-strategy-retries).

## DbContextTransaction

`DbContextTransaction<TDbContext>` implements `IStateUnit` and `ITransactionUnit` for EF Core.
It uses one `DbContext` for the save and for the transaction.
The class is in the `DannyGoodacre.Cqrs.EntityFrameworkCore` package.

```shell
dotnet add package DannyGoodacre.Cqrs.EntityFrameworkCore
```

It has these behaviors:

- `SaveChangesAsync` calls `DbContext.SaveChangesAsync`.
- `HasActiveTransaction` returns `true` if `DbContext.Database.CurrentTransaction` is not `null`.
- `ExecuteInTransactionAsync` opens a transaction on the `DbContext`, runs the operation, and commits if the result is a success.
- If a transaction is already open, it does not open a second transaction. See [Nested transactions](#nested-transactions).
- If the result is a failure, it rolls back the transaction and clears `DbContext.ChangeTracker`.
- If the operation throws an exception, it clears `DbContext.ChangeTracker` and throws the exception again. The transaction is not committed, so the database rolls it back when the transaction is disposed.

### Registration

`AddEntityFrameworkUnits<TDbContext>()` registers one scoped `DbContextTransaction<TDbContext>`.
It resolves `IStateUnit` and `ITransactionUnit` to that instance.

The example project calls it in `AddCustomerOnboarding`. [README.md](../README.md#4-register-the-services) shows the full method.
The caller supplies the database configuration. From [`Program.cs`](../../DannyGoodacre.Cqrs.Examples/Program.cs):

```csharp
services.AddSingleton(_ =>
{
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    return connection;
});

services.AddCustomerOnboarding((provider, options) =>
    options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
```

An in-memory SQLite database exists while its connection is open.
The container owns the connection as a singleton and disposes it with the service provider. Each scope creates a new `DbContext` on that connection.

For a server database, configure the provider and its retry strategy in the same place. For example, with SQL Server:

```csharp
services.AddCustomerOnboarding((_, options) => options.UseSqlServer(
    configuration.GetConnectionString("Customers"),
    sql => sql.EnableRetryOnFailure()));
```

Read the connection string from configuration. Do not write it in the source code.

> **Warning:** If you register the units yourself, do not register two separate instances for `IStateUnit` and `ITransactionUnit`. Child state commands must save on the same `DbContext` that holds the transaction.

> **Note:** The pipeline resolves `IStateUnit` and `ITransactionUnit` for all command handlers, also for plain `ICommand` handlers. Register both interfaces in each application that sends commands.

### The example database

From [`Persistence/ExamplesDbContext.cs`](../../DannyGoodacre.Cqrs.Examples/Persistence/ExamplesDbContext.cs):

```csharp
modelBuilder.Entity<Customer>(customer =>
{
    customer.HasKey(c => c.Id);
    customer.Property(c => c.Id).ValueGeneratedOnAdd();
    customer.HasIndex(c => c.PublicId).IsUnique();
    customer.HasIndex(c => c.Email).IsUnique();
});
```

`Id` is an integer primary key. `ValueGeneratedOnAdd` tells EF Core that the database generates it during the insert.
`PublicId` has a unique index. The handler generates it with `Guid.NewGuid()`, so the handler can return it. See [Entity keys](COMMANDS-AND-QUERIES.md#entity-keys).
Subscriptions use the same two keys. The subscription's foreign key, `CustomerId`, refers to the customer's integer `Id`.

The model also seeds two plans, `BASIC` and `PREMIUM`, with `HasData`. `EnsureCreatedAsync` creates the tables and the seed rows.

### Repositories

The example handlers use repositories. The repositories add and read entities on the scoped `DbContext`.
They do not call `SaveChangesAsync`. The pipeline calls it.

From [`Persistence/Repositories.cs`](../../DannyGoodacre.Cqrs.Examples/Persistence/Repositories.cs):

```csharp
public sealed class CustomerRepository(ExamplesDbContext dbContext) : ICustomerRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => dbContext.Customers.AnyAsync(c => c.Email == email, cancellationToken);

    public void Add(Customer customer)
        => dbContext.Customers.Add(customer);

    // The customer must exist. A missing row is a fault, so SingleAsync throws.
    public Task<int> GetIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => dbContext.Customers
            .Where(c => c.PublicId == publicId)
            .Select(c => c.Id)
            .SingleAsync(cancellationToken);

    public Task<CustomerDetails?> FindDetailsAsync(Guid publicId, CancellationToken cancellationToken = default)
        => dbContext.Customers
            .AsNoTracking()
            .Where(c => c.PublicId == publicId)
            .Select(c => new CustomerDetails(
                c.PublicId,
                c.Name,
                c.Email,
                dbContext.Subscriptions
                    .Where(s => s.CustomerId == c.Id)
                    .Select(s => s.PlanCode)
                    .FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);
}
```

`Add` only tracks the entity. `FindDetailsAsync` uses `AsNoTracking()` and projects to the result type with `Select()`.
`GetIdAsync` converts a public ID to the database ID. The onboarding handler uses it after the child command has saved the customer.

## Nested transactions

A database connection can have only one active transaction in EF Core.
`DbContextTransaction<TDbContext>` prevents a second transaction in these steps:

1. The outermost `ITransactedStateCommand` calls `ExecuteInTransactionAsync`. No transaction is open, so the unit opens one.
2. A child `IStateCommand` calls only `SaveChangesAsync`. It does not call `ExecuteInTransactionAsync`.
3. EF Core sees the open transaction. `SaveChangesAsync` writes inside it and does not commit.
4. A child `ITransactedStateCommand` calls `ExecuteInTransactionAsync`. The unit sees the open transaction. It runs the operation inside that transaction and does not start a new one.
5. The outermost call commits or rolls back all changes.

The example test `HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail` counts the transactions with an EF Core interceptor.
Onboarding calls `SaveChangesAsync` two times, but only one transaction starts and commits. See [TESTING.md](TESTING.md#transacted-command-tests).

## Failure and rollback

| Event | Action of `DbContextTransaction<TDbContext>` | Pipeline result |
| --- | --- | --- |
| Handler returns a failure result | Rolls back. Clears the change tracker. | The handler result |
| Handler throws an exception | Does not commit. Clears the change tracker. Throws again. | `InternalError` |
| `SaveChangesAsync` throws an exception | Does not commit. Clears the change tracker. Throws again. | `InternalError` |
| The operation is canceled | Does not commit. Clears the change tracker. Throws again. | `Canceled` |
| All steps succeed | Commits. | The handler result |

The unit clears `DbContext.ChangeTracker` after a failure. The reason is that the tracker still holds the entities from the failed steps.
If the scope uses the `DbContext` again, those entities are not saved by accident.

## Execution strategy retries

EF Core can retry an operation after a transient database fault. You turn on this feature with `EnableRetryOnFailure`.
EF Core then uses a retrying execution strategy.

A retrying strategy does not permit a transaction that the user opens directly.
EF Core throws `InvalidOperationException` if you call `BeginTransactionAsync` outside the strategy.
You must put the full transaction inside `IExecutionStrategy.ExecuteAsync`.

`ITransactionUnit` gets the work as a delegate for this reason. The unit does these steps:

1. It gets the execution strategy from `DbContext.Database.CreateExecutionStrategy()`.
2. It calls `strategy.ExecuteAsync`. Inside the strategy, it opens the transaction, runs the operation, and commits.
3. If a transient fault occurs, it rolls back and clears the change tracker.
4. The strategy runs step 2 again with a clean change tracker. This includes the handler and all child commands.

### State commands and retries

A standalone `IStateCommand` does not use `ITransactionUnit`. EF Core retries its `SaveChangesAsync` call as a unit, but it does not run the handler again.
This is enough for a command with one save. [TRADEOFFS.md](TRADEOFFS.md#state-commands-rely-on-ef-core-retries-for-the-save-only) explains the limits and when to use an `ITransactedStateCommand` instead.

### Rules for retry-safe handlers

The handler of an `ITransactedStateCommand` can run more than one time for one request. Obey these rules:

- Do not call external services in the handler. Put these calls in a post-commit hook. The hook runs one time, after the commit.
- Do not keep state in handler fields between calls.
- Generate keys inside the handler. Each attempt then uses new keys.
- Read all data from the database inside the operation. Do not use entities that you loaded before the transaction.

### Custom implementation

To use a different data store, or to change the behavior of the EF Core unit, implement `ITransactionUnit` yourself.
Use [`DbContextTransaction.cs`](../../DannyGoodacre.Cqrs.EntityFrameworkCore/DbContextTransaction.cs) as the reference. It is about 60 lines.

Your implementation must do these things:

1. If a transaction is already open, run the operation inside it. Do not start a second transaction.
2. Otherwise, start a transaction inside the execution strategy of the data store, if it has one.
3. Commit if the result is a success. Roll back if the result is a failure.
4. If the operation throws an exception, do not commit. Clear the tracked changes and throw the exception again.
5. Implement `HasActiveTransaction`, so the pipeline can detect a transaction that it did not start.
6. Register one scoped instance for both `IStateUnit` and `ITransactionUnit`, in the same way as `AddEntityFrameworkUnits<TDbContext>()`.

## Related documents

- [EXAMPLES.md](EXAMPLES.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [RESULT-HANDLING.md](RESULT-HANDLING.md)
- [TESTING.md](TESTING.md)
