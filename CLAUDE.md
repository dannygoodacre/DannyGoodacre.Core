# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository

.NET 10 libraries, each a separate NuGet package: `DannyGoodacre.Primitives` (result types), `DannyGoodacre.Cqrs` (decorator pipeline), `DannyGoodacre.Cqrs.EntityFrameworkCore`, `DannyGoodacre.Testing`, `DannyGoodacre.Cqrs.Testing`, and `DannyGoodacre.Identity` (accounts, invite code, and admins on ASP.NET Core Identity), plus the `DannyGoodacre.Cqrs.Examples` app and the `DannyGoodacre.Identity.Example` web API (SQLite, EF migrations, Scalar at `/scalar` in Development). Only projects in `DannyGoodacre.Core.slnx` are part of the codebase; other folders under `src/` and `tests/` are untracked leftovers.

## Commands

```shell
dotnet build
dotnet test
dotnet test --project tests/DannyGoodacre.Cqrs.Tests --filter-method "*WhenCommandIsPlain*"   # or --filter-class "<FullName>"
dotnet test --coverage --coverage-output-format cobertura --results-directory ./coverage_data  # as CI runs it
dotnet run --project src/DannyGoodacre.Cqrs.Examples
```

Tests use xUnit v3 on Microsoft.Testing.Platform. `global.json` opts `dotnet test` into it (the .NET 10 SDK rejects these projects otherwise), and coverage comes from `Microsoft.Testing.Extensions.CodeCoverage`; VSTest collectors such as `coverlet.collector` do not work.

## Releases

- Package versions live in each `.csproj` and are kept in step. `publish.yml` pushes all six to GitHub Packages on every push to `main` with `--skip-duplicate`, so a release needs a version bump.
- Each package packs its `README.md`. `publish.yml` rewrites relative links to absolute GitHub URLs before packing, so keep links relative in the repository.

## Architecture

- **Primitives:** each generic failure record (`Conflict<T>`, ...) derives from its non-generic one, so non-generic patterns match both. `IsSuccess`/`IsFailure` are default interface members, only visible through an `IResult`-typed variable. `MapFailure` is a closed `switch`; a new outcome type must be added to it. Code and docs create results with constructors; the `Result`/`Result<T>` factory tests deliberately use the factories.
- **Pipeline:** `AddCqrsHandlers` (`ServiceCollectionExtensions.cs`) scans assemblies, registers handlers, validators, and post-commit hooks, then wraps each handler: State → Transaction → Validation → Logging (outermost). Only `IStateCommand`/`ITransactedStateCommand` handlers get the State and Transaction decorators. Each call wraps only the handlers it added. Invalid setups throw at startup; the pipeline never throws to callers, converting exceptions to `InternalError` or `Canceled`.
- **Post-commit hooks** of child commands are deferred to the outermost commit by the internal scoped `PostCommitScope`; hook failures are logged, never returned.
- **Logging** uses `[LoggerMessage]` methods in `Decorators/LoggerExtensions.cs` with IDs from `CqrsLogEvent`; a new message needs both.
- **Persistence:** `IStateUnit` and `ITransactionUnit` must resolve to one scoped instance so child commands save inside the outer transaction. `DbContextTransaction` runs inside EF Core's execution strategy, so a retry replays the whole handler.
- **Identity:** a thin layer over stock ASP.NET Core Identity; configuration stays in Identity's own options, and the package changes only the security stamp interval and the cookie's redirect events. It does not depend on Cqrs, because Identity's managers save through their stores. Registration requires the invite code (one row in the internal `InviteCodes` table). Disabling an account uses Identity lockout until `UserExtensions.DisabledUntil` plus a security stamp change. Admins are the `Admin` role, exposed as the `Admin` policy (`IdentityDefaults`); at least one enabled admin must remain. Folders match namespaces: the public API lives in the root `DannyGoodacre.Identity` namespace so consumers need one `using`; internals go in `Accounts/`, `Data/`, and `Endpoints/`. Tests run each case on its own `TestApp` (test server, SQLite in-memory) rather than `WebApplicationFactory`, so options can differ per test.
- **Testing packages:** decorators are `internal`, exposed to `DannyGoodacre.Cqrs.Testing` and `DannyGoodacre.Cqrs.Tests` via `InternalsVisibleTo`. Every mock in a test context must use the context's `MockBehavior`; `TestContextMockBehaviorTests` enforces this. `DannyGoodacre.Testing` depends only on Primitives and `xunit.v3.assert`.

## Documentation

- Each package `README.md` is the reference for its own API; `src/DannyGoodacre.Cqrs/docs/` covers cross-package behavior and links to the READMEs rather than repeating them.
- Docs quote the example project and its tests by file. Keep quotes in sync with the code, and the sample output in `docs/EXAMPLES.md` matching a real run.
- Docs cover using the packages, not general EF Core, testing, or coding practice.

# .NET & C# Conventions

## Language & C# Features
- **Modern C# Standards:** Target C# 14. Use primary constructors, target-typed `new()`, pattern matching, `field` property accessors, and file-scoped namespaces (`namespace MyNamespace;`).
- **Types & Immutability:** Prefer `record` or `readonly struct` for DTOs and value objects. Use `init`-only properties over standard setters.
- **Nullability:** Treat nullable reference types strictly (`<Nullable>enable</Nullable>`). Avoid suppressing warnings with `!` unless proven safe.

## Async & Resource Management
- **Async Signature:** Always append `Async` to asynchronous method names (`GetUserAsync`).
- **Cancellation:** Pass `CancellationToken cancellationToken = default` as the last parameter in all async/I-O methods.
- **ValueTask:** Use `ValueTask` for async methods that frequently complete synchronously (e.g., hot path cache lookups).
- **Resource Disposal:** Use `await using` or `using` statement scope declarations rather than nested `using (...) { }` blocks.

## Architecture & EF Core
- **No Abstraction Over EF Core:** Do not create generic repository (`IRepository<T>`) or Unit of Work wrappers over `DbContext`. Use EF Core directly or via MediatR / Vertical Slices.
- **EF Core Performance:** Always use `.AsNoTracking()` for read-only queries. Avoid `Include()` over-fetching; use `.Select()` projections to map directly to DTOs.
- **Result Pattern:** Prefer returning a strongly-typed `Result<T>` model for validation/domain failures rather than throwing control-flow exceptions.

## Dependency Injection & API Design
- **Registration:** Use primary constructors for dependency injection. Do not write explicit private `readonly` fields and constructor assignments.
- **API Endpoints:** Prefer Minimal APIs / FastEndpoints / Carter over fat MVC controllers unless working in a legacy project.
- **Logging:** Use `ILogger<T>` with structured logging message templates (e.g., `_logger.LogInformation("Processing order {OrderId}", order.Id)`—do not use string interpolation `$""`).

## Comments
- **Only when necessary:** Write a comment only when the code cannot show *why* something was done, for example a non-obvious constraint, a workaround, or a decision that a future change could easily break. Most code needs no comment.
- **Never how:** Do not describe what the code does or how it works; the code shows that.
- **Never about changes:** Do not write comments that narrate an edit, its history, or how the code used to be (for example "now uses keyed services", "moved from X", "fixed: …"). That belongs in the commit message or PR description.
- **File overviews:** A short overview at the top of a file whose logic only makes sense across several methods is acceptable, such as the one in `ServiceCollectionExtensions.cs`. Keep it to the design decisions a change could break.
- **Example project:** `DannyGoodacre.Cqrs.Examples` is teaching code. Its explanatory comments are intentional and quoted word for word by the docs, so do not remove them under these rules; if you change one, update the docs that quote it.
