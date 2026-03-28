# TransactionCommandHandler

This handler executes business logic that persists changes to an underlying data store within an explicit transaction boundary in the same standardized manner as the [CommandHandler](./CommandHandler.md).

Like the command handler, this handler supports both void and valued responses.

It also provides an optional mechanism for ensuring data integrity, rolling back changes when the number of changes deviates from what is expected.

The interface `ITransactionUnit` provides an implementation of `ITransaction` to be used by the handler.

## Signature

```csharp
public abstract class TransactionCommandHandler<TCommand>(ILogger logger, ITransactionUnit transactionUnit)
    where TCommand : ICommand
```

## Members

| Name | Return Type | Required | Description |
| --- | --- | --- | --- |
| `ExpectedChanges` | `int` | No | The number of expected changes made to the underlying data store by the command. Do not set to disable validation. |

Additionally see [CommandHandler Members](./CommandHandler.md#members).

## Usage

The following example class implements `ITransactionUnit` and `ITransaction` using [EF Core](https://learn.microsoft.com/en-us/ef/core/). However, note that the abstraction is provider-agnostic.

```csharp
class TransactionUnit<TContext>(TContext context) : ITransactionUnit
    where TContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new Transaction(await context.Database.BeginTransactionAsync(cancellationToken));
}
```

```csharp
class Transaction(IDbContextTransaction transaction) : ITransaction
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
        => await transaction.CommitAsync(cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
        => await transaction.RollbackAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
```

Like in this example, the expected changes can be fixed in the command or set at runtime in the business logic itself. Not setting a value will cause the handler to not validate the number of expected changes.

```csharp
class DoThingTransactionHandler(ILogger<DoThingHandler> logger, ITransactionUnit transactionUnit, ISomeService service)
    : TransactionCommandHandler<DoThingCommand>(logger, transactionUnit)
{
    // ...

    protected override int ExpectedChanges => 123;

    protected async override Task<Result> InternalExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    {
        // ...
    }
}
```
