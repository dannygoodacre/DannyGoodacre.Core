[SCOPE RESTRICTION]
You must ONLY read, search, or inspect files within the following directory:
- ./src/DannyGoodacre.Cqrs
- ./src/DannyGoodacre.Cqrs.Examples
- ./tests/DannyGoodacre.Cqrs.Examples.Tests

Do NOT search, inspect, or modify any files outside these directories.

Task: Using the code and the following summary for context, please write technical documentation in the form of markdown files for the DannyGoodacre.Cqrs package.

### Summary
Marker Interfaces and Compatibility
- ICommand: The base interface for commands that do not save data to a database.
- IStateCommand: A command that changes state. The pipeline automatically calls SaveChangesAsync after the handler runs. This command generates database IDs. It operates safely inside an outer transaction.
- ITransactedStateCommand: A command that changes state inside an explicit database transaction. Use this to orchestrate multiple child commands or queries. If any step fails, the transaction rolls back.
- IQuery: A read-only operation. It must not change state.
- IPostCommitHook: An operation that runs only after a state command successfully commits data to the database. Use this for side effects like sending emails.

Execution Pipeline Order
- Command Pipeline: Logging Decorator -> Validation Decorator -> Transaction Decorator (if ITransactedStateCommand) -> State Decorator (if IStateCommand) -> Core Handler.
- Query Pipeline: Logging Decorator -> Validation Decorator -> Core Handler.

Pipeline Components and Decorators
- Validation: The validation decorator evaluates the request with IValidator<T>. If ValidationState contains errors, the decorator stops the pipeline immediately and returns an Invalid result. It does not call the database or the core handler.
- Persistence: The transaction and state decorators use ITransactionUnit and IStateUnit. The DbContextTransactionUnit implements these interfaces for Entity Framework Core. It prevents nested transactions. On failure or exception, it rolls back the transaction and clears the DbContext.ChangeTracker.
- Logging: The logging decorator records the start of a request, the validation failures, the execution errors, and the total execution time.

Result Primitives
- Handlers must not throw exceptions for domain logic control flow.
- Handlers must return IResult or IResult<T> from the DannyGoodacre.Primitives namespace.
- Valid return types are Success, DomainError, InternalError, Invalid, Conflict, NotFound, and Canceled.

### Output File Structure
Generate the following distinct Markdown files:
1. README.md - Quick Start, package installation, core concepts, and high-level feature overview.
2. ARCHITECTURE.md - Pipeline flow (Logging -> Validation -> Transaction -> State -> Handler), decorator wrapping order, and design principles.
3. COMMANDS-AND-QUERIES.md - Detailed guide on ICommand, IStateCommand, ITransactedStateCommand, IQuery<T>, and IPostCommitHook.
4. PERSISTENCE.md - Integration guide for DbContextTransactionUnit, IStateUnit, ITransactionUnit, and EF Core execution strategy retry handling.
5. RESULT-HANDLING.md - Usage patterns for IResult and IResult<T> primitives (Success, DomainError, Invalid, NotFound, InternalError).

### Documentation Guidelines & Constraints
- **Style Standard:** Follow ASD-STE100 (Simplified Technical English) rules. Use short, active-voice sentences. Avoid fluff, metaphors, jargon, or complex adverbs (e.g., use "in a single operation" instead of "atomically").
- **Code Examples:**
    - All C# examples must be fully functional, realistic domain workflows (e.g., user onboarding, order processing).
    - Show proper Dependency Injection configuration using `services.AddCqrsHandlers()`.
    - Demonstrate handling `IResult` outputs without throwing control-flow exceptions.
    - Include matching xUnit pipeline tests for commands and queries.
- **Technical Accuracy:**
    - Explicitly explain why `IStateCommand` is safe inside an outer `ITransactedStateCommand` without creating nested transactions.
    - Detail how `ValidationState` short-circuits the pipeline before hitting database state or handlers.
    - Explain how `IPostCommitHook` delays execution until state changes are saved to the database.
