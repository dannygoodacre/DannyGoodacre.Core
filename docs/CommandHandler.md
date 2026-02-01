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

TODO: Defining interface, registering service.

## Example
