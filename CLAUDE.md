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

