# Post-commit hooks

A post-commit hook runs a side effect after a state command's data is committed. Use hooks for actions outside the database, such as emails, messages, or cache updates.

## Hook interfaces

| Command handler | Hook interface | Hook method |
| --- | --- | --- |
| `ICommandHandler<TCommand>` | `IPostCommitHook<TCommand>` | `AfterCommitAsync(TCommand, IResult, CancellationToken)` |
| `ICommandHandler<TCommand, TResult>` | `IPostCommitHook<TCommand, TResult>` | `AfterCommitAsync(TCommand, IResult<TResult>, CancellationToken)` |

`AddCqrsHandlers` finds and registers hooks. A command can have several; they run in the order `AddCqrsHandlers` discovers them, so they should not depend on each other's order. A hook must target an `IStateCommand` or `ITransactedStateCommand`, and its interface must match the handler's: `IPostCommitHook<TCommand>` for a handler that returns no value, and `IPostCommitHook<TCommand, TResult>` with the same `TResult` for one that does. `AddCqrsHandlers` throws `InvalidOperationException` at startup for any other hook, because it would never run. The handler must be registered in the same call to `AddCqrsHandlers` as the hook, or an earlier one. A hook derived from a non-abstract hook of the same command also throws, because the base would run as well; make the base abstract, or remove the inheritance to run them as separate hooks.

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

The pipeline calls hooks only after a success, so `result` is always the handler's success result. Use it to read the returned value, such as the new customer's ID.

### Generic hooks

A hook must be a closed type: `AddCqrsHandlers` throws for a non-abstract open generic hook, because the checks above need the command type. An abstract generic base class works through the closed hooks derived from it. From [`Auditing/AuditHook.cs`](../../DannyGoodacre.Cqrs.Examples/Auditing/AuditHook.cs):

```csharp
// An abstract generic base hook. AddCqrsHandlers registers only the closed hooks derived from it, because its startup
// checks need the command type: a hook must target a state command whose handler has a matching signature.
// A non-abstract open generic hook would make AddCqrsHandlers throw at startup.
public abstract class AuditHook<TCommand, TResult>(ILogger<TCommand> logger) : IPostCommitHook<TCommand, TResult>
    where TCommand : ICommand
{
    public Task AfterCommitAsync(TCommand command, IResult<TResult> result, CancellationToken cancellationToken = default)
    {
        logger.LogCommitted(typeof(TCommand).Name);

        return Task.CompletedTask;
    }
}

// A child command's hooks wait for the outermost commit, so onboarding logs both commands after its transaction
// commits, and neither if it rolls back.
public sealed class RegisterCustomerAuditHook(ILogger<RegisterCustomerCommand> logger)
    : AuditHook<RegisterCustomerCommand, Guid>(logger);

public sealed class OnboardCustomerAuditHook(ILogger<OnboardCustomerCommand> logger)
    : AuditHook<OnboardCustomerCommand, Guid>(logger);
```

## Delivery guarantee

A hook is **best-effort**: it runs **at most once**, **after the commit**, and never after a rollback. It is not durable, so if the process stops between the commit and the hook, the side effect is lost.

Hooks guarantee ordering, not durability. A side effect that must never be lost needs to be recorded inside the transaction instead, for example as an outbox row; a hook can then trigger its delivery.

## When hooks run

Hooks are dependencies of the persistence decorators, not of the handler, so the handler cannot call them early.

- **`IStateCommand`:** the state decorator runs them after `SaveChangesAsync` completes.
- **`ITransactedStateCommand`:** the transaction decorator runs them after `ExecuteInTransactionAsync` returns a success, when the transaction is committed.
- **Child command inside a transaction:** see [Hooks on child commands](#hooks-on-child-commands).

Hooks do not run if validation fails, the handler returns a failure, or the save or transaction throws or is canceled. They run before the pipeline returns, so the caller waits for them.

The example test `HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail` proves the timing by recording whether a transaction is open when the hook runs. The rollback, conflict, and validation tests verify that no email is sent. See [`OnboardCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/OnboardCustomerPipelineTests.cs).

## Hook failures

The data is already committed when a hook runs, so a hook failure does not change the command result:

- `OperationCanceledException` is logged as event 3001 (`PostCommitHookCanceledMidExecution`, Warning).
- Any other exception is logged as event 3002 (`PostCommitFaulted`, Error).
- Either way, the pipeline runs the remaining hooks and returns the handler result.

Returning a failure would make the caller retry a command whose data is already committed. The caller therefore never learns of a hook failure; event 3002 is the place to find it.

Hooks receive `CancellationToken.None`, because a canceled request should not stop a side effect of committed data.

## Hooks on child commands

An `IStateCommand` with hooks behaves correctly both on its own and as a child:

- **On its own**, its hooks run after its save.
- **Inside a transaction**, the decorator adds its hooks to a per-scope list (the internal `PostCommitScope`) instead of running them. After the outermost commit, the listed hooks run, before the outer command's own hooks. A rollback discards the list.
- **After a retry**, each attempt of the execution strategy starts with an empty list, so each hook runs once.

A child `ITransactedStateCommand` joins the outer transaction, so its hooks also wait for the outermost commit.

`HandleAsync_WhenChildCommandHasHook_RunsItOnlyAfterTheOuterCommit` adds a hook to the child `RegisterCustomerCommand`: it runs once after a successful onboarding, and not at all when the plan does not exist.

The deferred list lives in the service scope, so hooks depend on [one scope per unit of work](ARCHITECTURE.md#scopes-and-service-lifetimes).

## Transactions the pipeline did not start

The pipeline tracks only the transactions it starts. It cannot defer hooks to one opened with `Database.BeginTransactionAsync` or an ambient `TransactionScope`, because no decorator runs at that commit.

If hooks are about to run while such a transaction is open, the pipeline logs event 3003 (`PostCommitHookInUnmanagedTransaction`, Warning) and runs them anyway. The hooks then run before the real commit, and still run if that transaction later rolls back. Skipping them instead would hide the problem and lose the side effect.

The pipeline detects the open transaction through [`IStateUnit.HasActiveTransaction`](PERSISTENCE.md#istateunit), and reads it only when hooks are about to run. An `IStateUnit` implementation that does not override it gets no detection. Strict mocks need it set up; the [DannyGoodacre.Cqrs.Testing contexts](../../DannyGoodacre.Cqrs.Testing/README.md) do this for you.

To keep hook timing correct, start transactions through an `ITransactedStateCommand`.
