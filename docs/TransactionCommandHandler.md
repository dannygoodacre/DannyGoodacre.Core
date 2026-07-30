# TransactionCommandHandler

This handler executes business logic that persists changes to an underlying data store within an explicit transaction boundary in the same standardized manner as the [CommandHandler](./CommandHandler.md).

Like the command handler, this handler supports both void and valued responses.

It also provides an optional mechanism for ensuring data integrity, rolling back changes when the number of changes deviates from what is expected.

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

The following example class implements `ITransactionUnit` using [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/). However, note that the abstraction is provider-agnostic.

```csharp
class TransactionUnit<TContext>(TContext context) : ITransactionUnit
    where TContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        where TResult : Result
    {
        if (Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        IExecutionStrategy strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);

            TResult result = await operation(cancellationToken);

            if (!result.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return result;
            }

            await transaction.CommitAsync(cancellationToken);

            return result;

        });
    }
}
```

The expected changes can be fixed in the command or set at runtime in the business logic itself. Not setting a value will cause the handler to not validate the number of expected changes.

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
