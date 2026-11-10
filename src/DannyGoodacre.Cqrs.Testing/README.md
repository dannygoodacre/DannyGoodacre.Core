# DannyGoodacre.Cqrs.Testing

Test contexts for [DannyGoodacre.Cqrs](../DannyGoodacre.Cqrs/README.md) pipeline decorators. Each context builds one decorator around Moq mocks, so a test can check validation, saving, transactions, post-commit hooks, and logging without a container or database.

To test your own handlers through the full pipeline, use the approach in [Pipeline tests](../DannyGoodacre.Cqrs/docs/TESTING.md#pipeline-tests) instead.

## Installation

```shell
dotnet add package DannyGoodacre.Cqrs.Testing
```

> **Note:** The package is currently published to GitHub Packages only; publication to nuget.org is planned. See the [installation note](../DannyGoodacre.Cqrs/README.md#installation) for adding the feed.

The package is built on xUnit v3, Moq, and the `FakeLogger` from `Microsoft.Extensions.Diagnostics.Testing`. It includes [DannyGoodacre.Testing](../DannyGoodacre.Testing/README.md), so the result assertions are available too. All types are in the `DannyGoodacre.Cqrs.Testing` namespace.

## Contexts

Every context exposes `InnerMock` (the wrapped handler), `Logger` (a `FakeLogger`) with its `LogCollector`, and `CreatePipeline()`, which returns the decorator under test. Each takes an optional `MockBehavior` for its mocks.

| Context | Decorator under test | Extra members |
| --- | --- | --- |
| `CommandValidationTestContext<TCommand>` / `<TCommand, TResult>`, `QueryValidationTestContext<TQuery, TResult>` | Validation | `ValidatorMock`; `CreatePipeline(includeValidator)` |
| `StateCommandTestContext<TCommand>` / `<TCommand, TResult>` | State | `StateUnitMock`, `PostCommitHookMock`, `SetupSaveChangesAsync()`, `SetupPostCommitHook(...)`, `SetupHasActiveTransaction()`; `CreatePipeline(includePostCommitHook)` |
| `TransactedStateCommandTestContext<TCommand>` / `<TCommand, TResult>` | Transaction | `TransactionUnitMock`, `PostCommitHookMock`, `SetupExecuteInTransaction()`, `SetupPostCommitHook(...)`, `SetupHasActiveTransaction()`; `CreatePipeline(includePostCommitHook)` |

- `SetupExecuteInTransaction()` makes the transaction mock run the operation it receives, so the handler and the save run as they would in a real transaction.
- The state and transaction contexts set up `HasActiveTransaction` to return `false`, because a strict mock throws for a default interface member with no setup. Call `SetupHasActiveTransaction(true)` to simulate a transaction the pipeline did not start.
- Commands and queries used with the contexts must be classes, for example `record` rather than `record struct`.

## Example

```csharp
private readonly StateCommandTestContext<StateCommand, Guid> _context = new(MockBehavior.Strict);

[Fact]
public async Task HandleAsync_WhenInnerHandlerFails_DoesNotSaveStateOrExecuteHook()
{
    // Arrange
    var command = new StateCommand();

    _context.InnerMock
        .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Conflict<Guid>("Database validation error"));

    var pipeline = _context.CreatePipeline(includePostCommitHook: true);

    // Act
    IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

    // Assert
    result.ShouldBeConflict("Database validation error");

    _context.StateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
```

## Log assertions

`LogCollector.ShouldHaveLogged(CqrsLogEvent, LogLevel)` asserts that the decorator wrote a [log event](../DannyGoodacre.Cqrs/docs/ARCHITECTURE.md#log-events), and returns the record for further checks:

```csharp
_context.LogCollector.ShouldHaveLogged(CqrsLogEvent.SaveChangesFaulted, LogLevel.Error);
```
