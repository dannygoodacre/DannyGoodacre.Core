# Examples

The example project shows a customer onboarding workflow.
It uses DannyGoodacre.Cqrs with EF Core and an in-memory SQLite database.
The test project sends each request through the full pipeline to that database.

| Project | Path |
| --- | --- |
| Console application | [`src/DannyGoodacre.Cqrs.Examples`](../../DannyGoodacre.Cqrs.Examples) |
| Pipeline tests | [`tests/DannyGoodacre.Cqrs.Examples.Tests`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests) |

The other documents quote this project. Each quote names its source file.

## Run the examples

Run these commands from the repository root:

```shell
dotnet run --project src/DannyGoodacre.Cqrs.Examples
dotnet run --project tests/DannyGoodacre.Cqrs.Examples.Tests
```

The console application writes the pipeline logs and one line for each result:

```text
Registered ann@example.com as customer 01a1a2f8-8b91-7f81-829b-d7fbcc848dde.
Onboarded jane@example.com as customer 01a1a2f8-8c15-7e48-aa46-3c3517be13c4.
Conflict: A customer with this email address already exists.
Plan 'GOLD' does not exist. No data was saved.
The request is not valid:
Name:
  - Name is required.
Email:
  - Email format is invalid.
PlanCode:
  - Plan code is required.
Ann Lee <ann@example.com> is on plan (none).
Jane Doe <jane@example.com> is on plan PREMIUM.
Customer 01a1a2f8-8c67-700e-a58c-ceaf50f7def9 does not exist.
```

The identifiers change each time you run the application.

## Domain

| Entity | Description |
| --- | --- |
| `Customer` | A registered customer. The email address is unique. |
| `Plan` | A subscription plan. The database contains two plans: `BASIC` and `PREMIUM`. |
| `Subscription` | Connects one customer to one plan. |

Customers and subscriptions have a database-generated integer `Id` and a `Guid` `PublicId` that the command creates.
Callers only see `PublicId`. Plans use their code as the key. See [Entity keys](COMMANDS-AND-QUERIES.md#entity-keys).

## Requests

| Request | Marker | Result | What it shows |
| --- | --- | --- | --- |
| `RegisterCustomerCommand` | `IStateCommand` | `IResult<Guid>` | Automatic save, a validator, and a `Conflict` result. |
| `GetCustomerQuery` | `IQuery<CustomerDetails>` | `IResult<CustomerDetails>` | A read-only projection and a `NotFound` result. |
| `GetPlanQuery` | `IQuery<PlanDetails>` | `IResult<PlanDetails>` | A query that a transacted command calls. |
| `OnboardCustomerCommand` | `ITransactedStateCommand` | `IResult<Guid>` | One transaction around a child command and a child query, a rollback, `MapFailure`, and a post-commit hook. |

## Onboarding workflow

`OnboardCustomerCommand` registers a customer and subscribes the customer to a plan in one transaction.
After the commit, a post-commit hook sends a welcome email.

```mermaid
sequenceDiagram
    participant Caller
    participant Pipeline as Onboard pipeline
    participant Register as RegisterCustomerCommand pipeline
    participant Plan as GetPlanQuery pipeline
    participant Db as SQLite
    participant Hook as SendWelcomeEmailHook

    Caller->>Pipeline: HandleAsync(OnboardCustomerCommand)
    Pipeline->>Pipeline: Validate. Stop with Invalid if there are errors.
    Pipeline->>Db: BEGIN TRANSACTION
    Pipeline->>Register: HandleAsync(RegisterCustomerCommand)
    Register->>Db: SELECT (email in use?)
    Register->>Db: INSERT Customer (SaveChangesAsync, no commit)
    Register-->>Pipeline: Success(customerId)
    Pipeline->>Plan: HandleAsync(GetPlanQuery)
    Plan->>Db: SELECT Plan
    Plan-->>Pipeline: Success(plan) or NotFound
    alt All steps succeed
        Pipeline->>Db: INSERT Subscription (SaveChangesAsync)
        Pipeline->>Db: COMMIT
        Pipeline->>Hook: AfterCommitAsync
        Pipeline-->>Caller: Success(customerId)
    else A step fails
        Pipeline->>Db: ROLLBACK (the customer row is removed)
        Pipeline-->>Caller: The failure result
    end
```

[COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md#itransactedstatecommand) explains each step.

## Scenarios in Program.cs

| Scenario | Result | Data in the database after the request |
| --- | --- | --- |
| Register `ann@example.com` | `Success<Guid>` | One new customer. |
| Onboard `jane@example.com` on `PREMIUM` | `Success<Guid>` | One new customer and one subscription. The welcome email is sent. |
| Onboard `JANE@example.com` again | `Conflict<Guid>` from the child command | No change. |
| Onboard `john@example.com` on `GOLD` | `NotFound<Guid>` | No change. The transaction rolls back the customer row that step 1 wrote. |
| Onboard with empty fields | `Invalid<Guid>` | No change. No SQL command runs. |
| Get Ann and Jane | `Success<CustomerDetails>` | Not applicable. |
| Get an unknown customer | `NotFound<CustomerDetails>` | Not applicable. |

## Files

### Example project

| File | Contents |
| --- | --- |
| [`Program.cs`](../../DannyGoodacre.Cqrs.Examples/Program.cs) | Service configuration, the scenarios, and result handling, in one `Main` method and one scope. |
| [`ExamplesServiceCollectionExtensions.cs`](../../DannyGoodacre.Cqrs.Examples/ExamplesServiceCollectionExtensions.cs) | Dependency injection with `AddCqrsHandlers` and `AddEntityFrameworkUnits`. |
| [`Persistence/Entities.cs`](../../DannyGoodacre.Cqrs.Examples/Persistence/Entities.cs) | `Customer`, `Plan`, and `Subscription`. |
| [`Persistence/ExamplesDbContext.cs`](../../DannyGoodacre.Cqrs.Examples/Persistence/ExamplesDbContext.cs) | The EF Core model and the plan seed data. |
| [`Persistence/Repositories.cs`](../../DannyGoodacre.Cqrs.Examples/Persistence/Repositories.cs) | The SQLite data access for the handlers. |
| [`Customers/RegisterCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/RegisterCustomerCommand.cs) | The state command, its validator, and its handler. |
| [`Customers/GetCustomerQuery.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/GetCustomerQuery.cs) | The customer query, its validator, and its handler. |
| [`Plans/GetPlanQuery.cs`](../../DannyGoodacre.Cqrs.Examples/Plans/GetPlanQuery.cs) | The plan query, its validator, and its handler. |
| [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs) | The transacted command, its validator, its handler, and its post-commit hook. |
| [`Notifications/EmailSender.cs`](../../DannyGoodacre.Cqrs.Examples/Notifications/EmailSender.cs) | `IEmailSender` and a stub that writes a log entry. |

### Test project

| File | Contents |
| --- | --- |
| [`Infrastructure/ExamplesTestHost.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/Infrastructure/ExamplesTestHost.cs) | Builds the application with a private SQLite database for each test. |
| [`Infrastructure/DatabaseActivity.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/Infrastructure/DatabaseActivity.cs) | An EF Core interceptor that records SQL commands and transactions. |
| [`RegisterCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/RegisterCustomerPipelineTests.cs) | State command tests. |
| [`GetCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/GetCustomerPipelineTests.cs) | Query tests. |
| [`OnboardCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/OnboardCustomerPipelineTests.cs) | Transacted command tests: commit, rollback, hook timing, and validation. |

[TESTING.md](TESTING.md) explains the tests.

## Related documents

- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md)
- [PERSISTENCE.md](PERSISTENCE.md)
- [TESTING.md](TESTING.md)
