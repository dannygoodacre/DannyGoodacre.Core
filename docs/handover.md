[SCOPE RESTRICTION]
You must ONLY read, search, or inspect files within the following directory:
- ./src/DannyGoodacre.Primitives
- ./src/DannyGoodacre.Cqrs
- ./src/DannyGoodacre.Cqrs.Examples
- ./tests/DannyGoodacre.Cqrs.Examples.Tests
- ./tests/DannyGoodacre.Primitives.Tests
- Any docs markdown files you are planning to write or have already written

Do NOT search, inspect, or modify any files outside these directories.

Task: Using the code and tests for context, please write technical documentation in the form of markdown files for the DannyGoodacre.Primitives package and update the documentation as necessary (to reduce repitition) for the DannyGoodacre.Cqrs package.

Please note any bugs you identify in the code in a separate BUGS.md file.

### Result Primitives
- Handlers must not throw exceptions for domain logic control flow.
- Handlers must return IResult or IResult<T> from the DannyGoodacre.Primitives namespace.
- Valid return types are Success, DomainError, InternalError, Invalid, Conflict, NotFound, and Canceled.
 
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
