# DannyGoodacre.Core

A collection of libraries used across my .NET projects.

## DannyGoodacre.Cqrs

A lightweight framework implementing the [Command Query Separation](https://martinfowler.com/bliki/CommandQuerySeparation.html) pattern in .NET, providing a standardized way to handle application logic and maintain state integrity with a unified, result-oriented flow.

See the [Cqrs documentation](./src/DannyGoodacre.Cqrs/README.md) for a detailed guide to the various classes provided.
The [examples project](./src/DannyGoodacre.Cqrs.Examples) contains a working customer onboarding workflow on SQLite, with [pipeline tests](./tests/DannyGoodacre.Cqrs.Examples.Tests).

## DannyGoodacre.Cqrs.EntityFrameworkCore

An [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) integration for the `IStateUnit` and `ITransactionUnit` interfaces from `DannyGoodacre.Cqrs`. See [Persistence](./src/DannyGoodacre.Cqrs/docs/PERSISTENCE.md).

## DannyGoodacre.Primitives

Result and Result\<T\> classes for robust result handling and a validation state for standardized error responses. See the [Primitives documentation](./src/DannyGoodacre.Primitives/README.md).

## DannyGoodacre.Testing

Assertion methods tailored for `Result` types, built on [xUnit](https://xunit.net/) and [Moq](https://github.com/devlooped/moq).

## DannyGoodacre.Cqrs.Testing

Base classes for testing CQRS handlers defined using `DannyGoodacre.Cqrs`. See [Testing](./src/DannyGoodacre.Cqrs/docs/TESTING.md).

## TL;DR

### The Result Pattern

Instead of throwing exceptions, every handler returns an `IResult` or `IResult<T>`. The result is one of `Success`, `DomainError`, `NotFound`, `Conflict`, `Invalid`, `InternalError`, or `Canceled`.

### The Handlers

| Type | Purpose | Key Feature |
| --- | --- | --- |
| `IQuery<T>` | Read-only operations | Returns `IResult<T>` with data payload |
| `ICommand` | Operations that do not save data to the database | Best for external integrations |
| `IStateCommand` | Persistence-focused | The pipeline calls `SaveChangesAsync` after the handler |
| `ITransactedStateCommand` | Persistence in a single operation | Explicit transaction that rolls back if any step fails |
