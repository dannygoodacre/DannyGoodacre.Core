# CommandHandler

The `CommandHandler` is the base class for executing business logic with validation, consistent logging, and centralized error handling, providing a consistent response in the form of a `Result` instance.

This is to be used in cases where the business logic has side effects but does not necessarily persist any state changes.

## Signature

```csharp
public abstract class CommandHandler<TCommand>(ILogger logger) where TCommand : ICommand
```

## Abstract & Virtual Members

| Name | Return Type | Required | Description |
| ------ | ---- | - | ----------- |
| `CommandName` | `string` | Yes | A human-readable name used for structured logging. |
| `Validate` | `void` | No | Logic to inspect the content of `TCommand`. Use `validationState.AddError` to stop execution. |
| `InternalExecuteAsync` | `Task<Result>` | Yes | The core logic. This only runs if `Validate` passes. |

## Implementation

First implement the command marker interface `ICommand` for this specific command.

```csharp
record DoThingCommand : ICommand
{
    public required string Property { get; init; }
}
```

Inherit from `CommandHandler<TCommand>` and implement the necessary members.

```csharp
class DoThingHandler(ILogger<DoThingHandler> logger, ISomeService service) 
    : CommandHandler<DoThingCommand>(logger)
{
    protected override string CommandName => "Do Thing";

    protected override void Validate(ValidationState validationState, DoThingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Property))
        {
            validationState.AddError(nameof(command.Property), "Must not be null, empty, or whitespace.");
        }
    }

    protected async override Task<Result> InternalExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    {
        bool hasWorked = await service.RunAsync(command.Property);

        return hasWorked
            ? Result.Success()
            : Result.DomainError("Has not worked");
    }
}
```

## Execution

TODO: Separate file for this, covering all handlers?

### 1. Via an abstraction

Define a specific interface for the action:

```csharp
interface IDoThing
{
    Task<Result> ExecuteAsync(string property, CancellationToken cancellationToken = default);
}

// Inside DoThingHandler
public Task<Result> ExecuteAsync(string property, CancellationToken cancellationToken = default)
    => ExecuteAsync(new DoThingCommand
    {
        Property = property
    }, cancellationToken);
```

### 2. Directly

If you prefer to directly access the command, then simply expose the base `ExecuteAsync` method:

```csharp
// Inside DoThingHandler
public new Task<Result> ExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    => base.ExecuteAsync(command, cancellationToken);
```

## Service Registration

TODO: Separate file for this, covering all handlers?

To register the handlers, use the extension method `AddCommandHandlers`. This maps the handler as a scoped service to all implemented business interfaces and registers it as a concrete service.

TODO: Talk about the command handlers that also return values.
