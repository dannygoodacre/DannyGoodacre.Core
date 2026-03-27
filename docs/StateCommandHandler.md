# StateCommandHandler

This handler executes business logic that persists changes to an underlying data store in the same standardized manner as the [CommandHandler](./CommandHandler.md).

The `IStateUnit` interface provides an abstraction for the [Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html) pattern, persisting all changes upon the command returning a successful response.

## Signature

```csharp
public abstract class StateCommandHandler<TCommand>(ILogger logger, IStateUnit stateUnit) where TCommand : ICommand
```

## Members

See [CommandHandler](./CommandHandler.md).

## Implementation

The following example class implements the `IStateUnit` interface by wrapping an [EF Core](https://learn.microsoft.com/en-us/ef/core/) context. However, note that the abstraction is provider-agnostic.

```csharp
class StateUnit<TContext>(TContext context) : IStateUnit
    where TContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
```

The concrete implementation below demonstrates a standard handler where the business logic is decoupled from the persistence orchestration.

```csharp
class DoThingStateHandler(ILogger<DoThingHandler> logger, IStateUnit stateUnit, ISomeService service)
    : StateCommandHandler<DoThingCommand>(logger, stateUnit)
{
    // Omitted for brevity...

    protected async override Task<Result> InternalExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    {
        bool hasWorked = await service.RunAsync(command.Property);

        return hasWorked
            ? Result.Success()
            : Result.DomainError("Has not worked");
    }
}
```

State command handlers have access to the internal state unit property for manual control over the persistence logic when needed.
