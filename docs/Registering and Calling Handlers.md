# Service Registration

To register the handlers, use the extension methods `AddCommandHandlers` and `AddQueryHandlers`. These map the handlers as scoped services to all implemented business interfaces and also register them as concrete services.

# Calling a Handler

## Via an abstraction

Define a specific interface for the action, implementing it with the handler.

In this example, the caller passes in the command/query properties directly, constructing the command/query object internally. However, this is not strictly necessary and passing in the request object will work too.

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

## Directly

Simply expose the base `ExecuteAsync` method with a new public method, passing in a command/query request object directly.

```csharp
// Inside DoThingHandler
public new Task<Result> ExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    => base.ExecuteAsync(command, cancellationToken);
```
