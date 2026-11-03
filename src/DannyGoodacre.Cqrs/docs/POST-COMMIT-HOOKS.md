# Post-commit hooks

A post-commit hook runs a side effect after a state command saves its data.
Use hooks for actions outside the database, for example emails, messages, or cache updates.

## Delivery guarantee

A hook is **best-effort**. It runs **at most one time**, **after the commit**, and never after a rollback.
It is not durable. If the process stops between the commit and the hook, the side effect is lost.

Use a hook only where a rare loss is acceptable or recoverable, for example cache invalidation, real-time UI updates, metrics, or a non-critical email.
For side effects that must not be lost, such as payments or integration events, write an outbox row in the handler, inside the transaction. A separate process sends it. A hook can then wake that process.

[TRADEOFFS.md](TRADEOFFS.md#post-commit-hooks-are-best-effort) explains this decision.

## Hook interfaces

| Command handler | Hook interface | Hook method |
| --- | --- | --- |
| `ICommandHandler<TCommand>` | `IPostCommitHook<TCommand>` | `AfterCommitAsync(TCommand, IResult, CancellationToken)` |
| `ICommandHandler<TCommand, TResult>` | `IPostCommitHook<TCommand, TResult>` | `AfterCommitAsync(TCommand, IResult<TResult>, CancellationToken)` |

`AddCqrsHandlers` finds and registers hooks for you. You do not register them manually.
A command can have more than one hook. The pipeline runs all of them, in registration order.

A hook must target an `IStateCommand` or an `ITransactedStateCommand`. A plain `ICommand` saves no data, so `AddCqrsHandlers` throws `InvalidOperationException` at startup for a hook on a plain `ICommand`.

## Example: welcome email

The example sends a welcome email after onboarding commits.
From [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs):

```csharp
// Runs only after the transaction commits. If any step fails, the email is not sent.
public sealed class SendWelcomeEmailHook(IEmailSender emailSender)
    : IPostCommitHook<OnboardCustomerCommand, Guid>
{
    public Task AfterCommitAsync(
        OnboardCustomerCommand command,
        IResult<Guid> result,
        CancellationToken cancellationToken = default)
        => emailSender.SendWelcomeEmailAsync(command.Email, command.Name, cancellationToken);
}
```

The hook does not check `result`. The pipeline calls the hook only for a success result.

## How the pipeline delays the hook

The hook is a dependency of the persistence decorator, not of the handler.
The handler cannot call the hook. The decorator calls the hook only at the end of the persistence step:

- **`IStateCommand`:** The state decorator calls the hook after `SaveChangesAsync` completes.
- **`ITransactedStateCommand`:** The transaction decorator calls the hook after `ExecuteInTransactionAsync` returns a success. At that time, the transaction is committed.
- **Child command in a transaction:** The decorator does not run the hook after the child save. It adds the hook to a list for the request scope. After the outermost transaction commits, the hooks in the list run, before the hooks of the outer command. If the transaction rolls back, the list is discarded. See [Hooks on child commands](#hooks-on-child-commands).

The hook gets `CancellationToken.None`. The data is committed, so a canceled request must not stop the side effect.
If a hook needs a time limit, create its own token in the hook.

The hook does not run in these conditions:

- The validator finds errors.
- The handler returns a failure result.
- `SaveChangesAsync` or the transaction throws an exception, or is canceled.
- The command is a child of a transacted command, and the transaction rolls back.

### Proof in the example tests

`HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail` records the transaction state when the hook runs.
From [`OnboardCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/OnboardCustomerPipelineTests.cs):

```csharp
host.EmailSender
    .Setup(x => x.SendWelcomeEmailAsync("jane@example.com", "Jane Doe", It.IsAny<CancellationToken>()))
    .Callback(() => transactionOpenWhenHookRan = dbContext.Database.CurrentTransaction is not null)
    .Returns(Task.CompletedTask);
```

The test then asserts that `transactionOpenWhenHookRan` is `false`. Thus, the hook ran after the commit.
The rollback, conflict, and validation tests verify that the email sender is never called.

`HandleAsync_WhenChildCommandHasHook_RunsItOnlyAfterTheOuterCommit` adds a hook to the child `RegisterCustomerCommand`.
When onboarding succeeds, the child hook runs once, after the commit. When the plan does not exist, the child hook does not run.

## Hook failures

The data is already saved when the hook runs. Thus, a hook failure does not change the result of the command:

- If the hook throws `OperationCanceledException`, the pipeline logs event 3001 (`PostCommitHookCanceledMidExecution`, Warning).
- If the hook throws a different exception, the pipeline logs event 3002 (`PostCommitFaulted`, Error) with the exception.
- In both cases, the pipeline runs the next hook and returns the handler result.

The caller does not see the failure, so it does not send the command again and create duplicate data.
Monitor event 3002 to find failed side effects. For work that must not be lost, use an outbox. See [Hook rules](#hook-rules).

## Hooks on child commands

A child `IStateCommand` can run on its own or inside a transacted command. Its hooks are safe in both cases:

- **On its own:** the hooks run after its save.
- **Inside a transaction:** the hooks wait for the outermost commit. If the transaction rolls back, they do not run.
- **After a retry:** the execution strategy can run the transaction more than one time. Each attempt starts with an empty list, so each hook runs one time only.

A child `ITransactedStateCommand` joins the outer transaction. Its hooks also wait for the outermost commit.

## Scope and transaction rules

Deferred hooks depend on the same service scope as the `DbContext` and the transaction. Obey these rules:

- Use one service scope for each unit of work, for example each HTTP request or each message. Do not resolve handlers from the root provider.
- Do not run child commands in parallel in one scope.
- Do not share one scope between interleaved operations, for example in a Blazor Server circuit.
- Start transactions only through an `ITransactedStateCommand`.

The pipeline cannot defer hooks to a transaction that it did not start, for example one that you open with `Database.BeginTransactionAsync`, or an ambient `TransactionScope`.
If a hook is about to run while such a transaction is open, the pipeline logs event 3003 (`PostCommitHookInUnmanagedTransaction`, Warning) and runs the hook. The hook then runs before the real commit.
The pipeline detects the open transaction with `IStateUnit.HasActiveTransaction`. See [PERSISTENCE.md](PERSISTENCE.md#istateunit).

[TRADEOFFS.md](TRADEOFFS.md#the-pipeline-depends-on-one-scope-for-each-unit-of-work) explains what goes wrong when you break each rule.

## Hook rules

- Keep the hook short. The caller waits for the hook to complete.
- For work that must not be lost, use an outbox. See [Delivery guarantee](#delivery-guarantee).

## Related documents

- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [TRADEOFFS.md](TRADEOFFS.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [TESTING.md](TESTING.md)
