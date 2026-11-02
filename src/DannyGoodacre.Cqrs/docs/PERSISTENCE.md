# Persistence

This document tells you how to connect the pipeline to a database.
It describes `IStateUnit`, `ITransactionUnit`, and `DbContextTransactionUnit` for Entity Framework Core.
It also describes EF Core execution strategy retries.

## Contracts

The persistence decorators do not know about EF Core. They use two interfaces.

### IStateUnit

```csharp
public interface IStateUnit
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`SaveChangesAsync` writes all pending changes in the current scope to the data store.
It returns the number of entries that it wrote.

The state decorator calls `SaveChangesAsync` after a successful `IStateCommand` handler.

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

## DbContextTransactionUnit

`DbContextTransactionUnit` implements `IStateUnit` and `ITransactionUnit` for EF Core.
It uses one `DbContext` for the save and for the transaction.

It has these behaviors:

- `SaveChangesAsync` calls `DbContext.SaveChangesAsync`.
- `ExecuteInTransactionAsync` opens a transaction on the `DbContext`, runs the operation, and commits if the result is a success.
- If a transaction is already open, it does not open a second transaction. See [Nested transactions](#nested-transactions).
- If the result is a failure, or if the operation throws an exception, it rolls back the transaction.
- After a rollback, it clears `DbContext.ChangeTracker`.

### Registration

Register one scoped instance. Resolve both interfaces to that instance.

```csharp
using DannyGoodacre.Cqrs;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Shop"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<ITransactionUnit>(sp =>
    new DbContextTransactionUnit(sp.GetRequiredService<ShopDbContext>()));

builder.Services.AddScoped<IStateUnit>(sp => sp.GetRequiredService<ITransactionUnit>());

builder.Services.AddCqrsHandlers(typeof(PlaceOrderCommand).Assembly);
```

> **Warning:** Do not register two separate instances for `IStateUnit` and `ITransactionUnit`. Child state commands must save on the same `DbContext` that holds the transaction.

> **Note:** The pipeline resolves `IStateUnit` and `ITransactionUnit` for all command handlers, also for plain `ICommand` handlers. Register both interfaces in each application that sends commands.

### Repositories

The handlers in these documents use repositories. The repositories add and read entities on the scoped `DbContext`.
They do not call `SaveChangesAsync`. The pipeline calls it.

```csharp
using Microsoft.EntityFrameworkCore;
using Shop.Orders;

namespace Shop.Infrastructure;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(p => p.Sku);

        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<Order>().Property(o => o.Id).ValueGeneratedNever();
        modelBuilder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
    }
}

public sealed class EfProductRepository(ShopDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        => dbContext.Products.SingleOrDefaultAsync(p => p.Sku == sku, cancellationToken);
}

public sealed class EfOrderRepository(ShopDbContext dbContext) : IOrderRepository
{
    public void Add(Order order) => dbContext.Orders.Add(order);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Orders.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<OrderSummary?> FindSummaryAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderSummary(o.Id, o.CustomerId, o.ProductSku, o.Quantity, o.Total, o.Status))
            .SingleOrDefaultAsync(cancellationToken);
}
```

`ValueGeneratedNever` tells EF Core that the application sets the key.
The handler generates the key with `Guid.CreateVersion7()`. Thus, the key is in the handler result.

## Nested transactions

A database connection can have only one active transaction in EF Core.
`DbContextTransactionUnit` prevents a second transaction in these steps:

1. The outermost `ITransactedStateCommand` calls `ExecuteInTransactionAsync`. No transaction is open, so the unit opens one.
2. A child `IStateCommand` calls only `SaveChangesAsync`. It does not call `ExecuteInTransactionAsync`.
3. EF Core sees the open transaction. `SaveChangesAsync` writes inside it and does not commit.
4. A child `ITransactedStateCommand` calls `ExecuteInTransactionAsync`. The unit sees the open transaction. It runs the operation inside that transaction and does not start a new one.
5. The outermost call commits or rolls back all changes.

## Failure and rollback

| Event | Action of `DbContextTransactionUnit` | Pipeline result |
| --- | --- | --- |
| Handler returns a failure result | Rolls back. Clears the change tracker. | The handler result |
| Handler throws an exception | Rolls back. Clears the change tracker. Throws again. | `InternalError` |
| `SaveChangesAsync` throws an exception | Rolls back. Clears the change tracker. Throws again. | `InternalError` |
| The operation is canceled | Rolls back. Clears the change tracker. Throws again. | `Canceled` |
| All steps succeed | Commits. | The handler result |

The unit clears `DbContext.ChangeTracker` after a rollback. The reason is that the tracker still holds the entities from the failed steps.
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

### Rules for retry-safe handlers

The handler of an `ITransactedStateCommand` can run more than one time for one request. Obey these rules:

- Do not call external services in the handler. Put these calls in a post-commit hook. The hook runs one time, after the commit.
- Do not keep state in handler fields between calls.
- Generate keys inside the handler. Each attempt then uses new keys.
- Read all data from the database inside the operation. Do not use entities that you loaded before the transaction.

### Custom implementation

To use a different data store, implement `ITransactionUnit` yourself.
This example shows the full contract for EF Core. It includes execution strategy support.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Shop.Infrastructure;

public sealed class ShopTransactionUnit(ShopDbContext dbContext) : ITransactionUnit
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public Task<TResultWrapper> ExecuteInTransactionAsync<TResultWrapper>(
        Func<CancellationToken, Task<TResultWrapper>> operation,
        CancellationToken cancellationToken = default)
        where TResultWrapper : IResult
    {
        // A transaction is already open. Join it. Do not start a second one.
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return operation(cancellationToken);
        }

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(
            async ct =>
            {
                await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

                try
                {
                    TResultWrapper result = await operation(ct);

                    if (result.IsSuccess)
                    {
                        await transaction.CommitAsync(ct);
                    }
                    else
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        dbContext.ChangeTracker.Clear();
                    }

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    dbContext.ChangeTracker.Clear();

                    throw;
                }
            },
            cancellationToken);
    }
}
```

The rollback uses `CancellationToken.None`. The rollback must complete, also when the request is canceled.

## Related documents

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [RESULT-HANDLING.md](RESULT-HANDLING.md)
