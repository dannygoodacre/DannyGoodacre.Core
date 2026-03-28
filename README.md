# DannyGoodacre.Core

A lightweight framework implementing the [Command Query Separation](https://martinfowler.com/bliki/CommandQuerySeparation.html) pattern in .NET, providing a standardized way to handle application logic and maintain state integrity with a unified, result-oriented flow.

See the [docs](./docs/) for a detailed guide to the various classes provided.

## TL;DR

### The Result Pattern

Instead of throwing exceptions, every handler returns a `Result` or `Result<T>`, encapsulating the status of the operation, any returned data, any validation errors, and any exceptions.

### The Handlers

| Type | Purpose | Key Feature |
| --- | --- | --- |
| Query | Read-only operations | Returns `Result<T>` with data payload |
| Command | Side-effects only | Best for external integrations |
| StateCommand | Persistence-focused | Standardized `SaveChangesAsync` flow |
| TransactionCommand | Atomic persistence | Explicit transactions with `ExpectedChanges` guards |
