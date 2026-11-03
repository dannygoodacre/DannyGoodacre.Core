# Design tradeoffs

This document explains the main design decisions in DannyGoodacre.Cqrs, what each decision gives you, and what it costs.

## Post-commit hooks are best-effort

**Decision:** A post-commit hook runs at most one time, after the commit, and never after a rollback. It is not durable.

**Why:** The hard part of a side effect is the order: never before the commit, never after a rollback. Without hooks, a side effect goes in the handler, where it runs before the commit and also after a rollback, or in the caller, which must then know the side effects of every child command.

**Cost:** If the process stops between the commit and the hook, the side effect is lost. Use hooks only where a rare loss is acceptable or recoverable.

For side effects that must not be lost, write an outbox row in the handler, inside the transaction. A separate process sends it. A hook can then wake that process, so the message goes out without waiting for the next poll.

## Hook failures do not change the result

**Decision:** If a hook throws, the pipeline logs event 3002 and returns the original success result. Hooks get `CancellationToken.None`.

**Why:** The data is committed. A failure result would make the caller send the command again and create duplicate data. A canceled request must not stop a side effect of committed data.

**Cost:** The caller does not know about the failure. Monitor event 3002. A hook that can hang must create its own time limit.

## Child hooks wait for the outermost commit

**Decision:** When a state command runs inside a transacted command, its hooks wait in a list (`PostCommitScope`) until the outermost commit. A rollback discards the list. Each retry attempt starts with an empty list.

**Why:** Commands are designed to be composed. A reusable command with a hook must be safe as a child.

**Cost:** About 100 lines of internal code. The simpler alternative was to forbid hooks on commands that run as children, but that would make those commands unusable in a composition.

## The pipeline depends on one scope for each unit of work

**Decision:** `PostCommitScope`, the `DbContext`, and the transaction unit are scoped services. They must come from the same service scope.

**Why:** EF Core has the same requirement. A child command joins the outer transaction only if it uses the same `DbContext`. The pipeline follows the same rule, so it adds no new constraint.

**Cost:** Hooks work correctly only if you obey these rules:

| Rule | What goes wrong if you break it |
| --- | --- |
| Use one service scope for each unit of work, for example each HTTP request or each message. | Resolved from the root provider, the scope is shared by all requests. One request's hooks can follow another request's transaction. The `DbContext` is also shared, which EF Core does not support. |
| Do not run child commands in parallel in one scope. | `PostCommitScope` is not thread-safe. EF Core also forbids parallel work on one `DbContext`. |
| Do not share a scope between interleaved operations, for example in a Blazor Server circuit. | One operation's hooks can wait for another operation's transaction. Use a scope or a `DbContext` for each operation. |
| Start transactions only through `ITransactedStateCommand`. | See the next section. |

A child command that you send in a separate scope is not an error. Its `DbContext` is not in the outer transaction, so its save commits at once, and its hooks run at once. The hook timing matches the data.

## Transactions that the pipeline did not start are detected, not supported

**Decision:** `PostCommitScope` tracks only the transactions that the pipeline starts. `IStateUnit.HasActiveTransaction` reports the real transaction state. If a hook is about to run while a transaction that the pipeline did not start is still open, the pipeline logs event 3003 (Warning) and runs the hook.

**Why:** No decorator runs at the commit of a manual or ambient transaction, so the pipeline cannot defer hooks to it. A warning makes the problem visible. Skipping the hook would hide it and lose the side effect.

**Cost:**

- In that case, the hook runs before the real commit. If the manual transaction then rolls back, the hook has already run.
- `HasActiveTransaction` is a default interface member that returns `false`. An existing `IStateUnit` implementation still compiles, but it gets no detection until it implements the member. `DbContextTransaction<TDbContext>` implements it.
- A strict mock throws for a default interface member with no setup. Tests that use strict mocks of `IStateUnit` or `ITransactionUnit` must set up `HasActiveTransaction`. The `DannyGoodacre.Cqrs.Testing` contexts do this for you.
- The pipeline reads the property only when hooks are about to run. Commands without hooks never read it.

## State commands rely on EF Core retries for the save only

**Decision:** An `IStateCommand` gets a unit of work with no code from the developer: the handler changes tracked entities, and the pipeline calls `SaveChangesAsync` one time. The pipeline does not wrap a state command in an execution strategy. Only `ITransactedStateCommand` gets a full replay.

**Why:** With `EnableRetryOnFailure`, EF Core already retries each query and each `SaveChangesAsync` call as a unit. `SaveChangesAsync` writes all changes in one internal transaction. A command with one save is therefore atomic and retried without help. A child state command inside a transacted command runs inside the outer execution strategy, so the outer retry replays it.

**Cost:**

| Limit | What to do |
| --- | --- |
| A retry sends the same changes again. It does not run the handler again, so the handler's reads and decisions are not made again. | If the decision must be made again on fresh data after a fault, use an `ITransactedStateCommand`. |
| Another request can change the same rows between the handler's read and the save. Retries do not prevent this, and a transacted command at the default isolation level does not prevent it either. | Use a concurrency token or a unique constraint. A violation surfaces as a `DbUpdateException`, which the pipeline returns as `InternalError`. |
| If the connection fails after the database commits but before EF Core receives the confirmation, the retry sends the changes again. This also applies to transacted commands. | Generate unique keys in the command, for example `PublicId`, so that the repeated insert fails and does not create a second row. The caller then gets `InternalError` for committed data. |
| Retries need a retrying strategy in the provider. SQLite has none. A custom `IStateUnit` gets no retries from the pipeline. | Call `EnableRetryOnFailure` or the equivalent for your provider. A custom `IStateUnit` must retry in `SaveChangesAsync` itself. |

Use an `ITransactedStateCommand` when the command needs more than one save, or when the handler must run again on retry.

## Related documents

- [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md)
- [PERSISTENCE.md](PERSISTENCE.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
