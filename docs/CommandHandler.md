# CommandHandler

The most basic of the command handlers, providing command validation, a consistent response format, and centralized error handling for executing business logic that has side effects but does not persist any changes.

## Signature

```csharp
public abstract class CommandHandler<TCommand>(ILogger logger) where TCommand : ICommand
```

## Members

| Name | Type | Required | Description |
| ------ | ---- | - | ----------- |
| `CommandName` | `string` | Yes | A human-readable name used for logging. |
| `Validate` | `void` | No | An optional check of the `TCommand` instance. |
| `InternalExecuteAsync` | `Task<Result>` | Yes | The core business logic here. This is only run if validation succeeds. |

## Usage

Consider the following example implementation:

```csharp
class DoThingHandler(ILogger logger, IService service) : CommandHandler<DoThingCommand>(logger)
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

We have two ways of using this command:

1. Defining an interface:

    ```csharp
    interface IDoThing
    {
        Task<Result> ExecuteAsync(string property, CancellationToken cancellationToken = default);
    }
    ```

    Implementing this interface in the handler as follows:

    ```csharp
    {
        // Rest of DoThingHandler...

        public Task<Result> ExecuteAsync(string property, CancellationToken cancellationToken = default)
            => ExecuteAsync(new DoThingCommand
            {
                Value = value
            }, cancellationToken);
    }
    ```

2. Exposing the ExecuteAsync method directly:

    ```csharp
    {
        // Rest of DoThingHandler...

        public new Task<Result> ExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
            => base.ExecuteAsync(command, cancellationToken);
    }
    ```

TODO: Talk about registering the handlers using the included extension methods.
