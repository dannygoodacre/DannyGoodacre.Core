# Testing

How to test code that uses DannyGoodacre.Cqrs. Two packages help:

- [DannyGoodacre.Testing](../../DannyGoodacre.Testing/README.md) provides xUnit assertions for results, such as `Guid id = result.ShouldBeSuccess();` and `result.ShouldBeConflict("...")`.
- [DannyGoodacre.Cqrs.Testing](../../DannyGoodacre.Cqrs.Testing/README.md) builds a single pipeline decorator around Moq mocks, for testing the decorators' behavior without a container or database.

## Pipeline tests

To test a request through the full pipeline, register the application's services as the application does, including `AddCqrsHandlers` and the persistence units, and resolve the handler interface from a new scope for each request. Resolving the interface returns the decorated pipeline, so the test covers validation, saving, transactions, and hooks together.

- A hook failure does not change the command result. A mocked hook dependency that should not be called needs an explicit `Times.Never` check; a strict mock alone does not fail the test.
- To check that a hook runs after the commit, resolve the `DbContext` from the same scope as the handler and record whether a transaction is open when the hook runs:

```csharp
host.EmailSender
    .Setup(x => x.SendWelcomeEmailAsync("jane@example.com", "Jane Doe", It.IsAny<CancellationToken>()))
    .Callback(() => transactionOpenWhenHookRan = dbContext.Database.CurrentTransaction is not null)
    .Returns(Task.CompletedTask);
```

The example project's tests, in [`tests/DannyGoodacre.Cqrs.Examples.Tests`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests), use this approach with an in-memory SQLite database and an EF Core interceptor that records SQL commands and transactions. They cover saving, validation short-circuits, conflicts, nested transactions, rollback, and hook timing; [EXAMPLES.md](EXAMPLES.md#files) lists the files.

## Testing a handler without the pipeline

Constructing a handler directly bypasses every decorator: nothing validates, saves, or runs hooks. An orchestrator handler can be tested this way by giving it mocks of its child handler interfaces that return results.
