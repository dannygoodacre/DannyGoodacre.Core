# CommandHandler

The `CommandHandler` is the base class for executing business logic with validation, consistent logging, and centralized error handling, providing a consistent response in the form of a `Result` or `Result<T>` object.

This is to be used when the business logic has side effects but does not persist any state changes.

## Signature

```csharp
public abstract class CommandHandler<TCommand>(ILogger logger)
    where TCommand : ICommand
```

## Members

| Name | Return Type | Required | Description |
| --- | --- | --- | --- |
| `CommandName` | `string` | Yes | A human-readable name used for structured logging. |
| `Validate` | `void` | No | Logic to inspect the content of `TCommand`. Use `validationState.AddError` to stop execution. |
| `InternalExecuteAsync` | `Task<Result>` | Yes | The core logic. This only runs if `Validate` passes. |

## Usage

First implement `ICommand` for this specific command.

```csharp
record DoThingCommand : ICommand
{
    public required string Property { get; init; }
}
```

Then inherit from `CommandHandler<TCommand>` and implement the necessary members.

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

For a command that returns a value, instead inherit from `CommandHandler<TCommand, TResult>`. In this example, the returned type is an integer.

```csharp
class DoThingHandler(ILogger<DoThingHandler> logger, ISomeService service)
    : CommandHandler<DoThingCommand, int>(logger)
{
    // ...

    protected async override Task<Result<int>> InternalExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    {
        bool someNumber = await service.GetAsync(command.Property);

        return someNumber != 0
            ? Result.Success(someNumber)
            : Result.DomainError("Has not worked");
    }
}
```
