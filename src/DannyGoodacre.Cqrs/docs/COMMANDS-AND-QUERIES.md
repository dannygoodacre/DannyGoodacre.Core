# Commands and queries

How to choose a request marker and write handlers and validators. The examples come from the [example project](EXAMPLES.md); the result types are documented in [DannyGoodacre.Primitives](../../DannyGoodacre.Primitives/README.md).

## Select a marker interface

| Marker | Saves data | Transaction | Post-commit hooks | Use for |
| --- | --- | --- | --- | --- |
| `ICommand` | No | No | Not allowed | Actions with no database change, for example a call to an external API. |
| `IStateCommand` | Yes, the pipeline calls `SaveChangesAsync` | Never opens one; runs inside the outer transaction when used as a child | After the save | A change saved in one step. |
| `ITransactedStateCommand` | Yes, before the commit | One explicit transaction | After the commit | Workflows that run several child commands and queries as one unit. |
| `IQuery<T>` | No | No | Not available | Reads that return data. |

1. If the request returns data and changes nothing, use `IQuery<T>`.
2. If it changes data in one step, use `IStateCommand`.
3. If it runs more than one state-changing step, use `ITransactedStateCommand`.
4. If it changes no data in your database, use `ICommand`.

A type cannot implement both `IQuery<T>` and a state command marker; `AddCqrsHandlers` throws `InvalidOperationException` at startup. If a command implements both `IStateCommand` and `ITransactedStateCommand`, the transacted behavior wins.

## Handlers

| Request | Handler interface | Return type |
| --- | --- | --- |
| Command with no return value | `ICommandHandler<TCommand>` | `Task<IResult>` |
| Command with a return value | `ICommandHandler<TCommand, TResult>` | `Task<IResult<TResult>>` |
| Query | `IQueryHandler<TQuery, TResult>` | `Task<IResult<TResult>>` |

`TResult` is non-nullable; return `NotFound` instead of `null`. `AddCqrsHandlers` finds and registers every handler in the assemblies you give it.

The persistence decorators read `IsSuccess`, so returning a failure result is enough to stop the save, roll back the transaction, and skip the post-commit hooks. The pipeline also creates some results itself, which callers should expect alongside the handler's own:

| Result | Created by | When |
| --- | --- | --- |
| `Invalid` | Validation decorator | A validator reports errors. See [Validators](#validators). |
| `Canceled` | Logging and persistence decorators | The caller's cancellation token is canceled. |
| `InternalError` | Logging and persistence decorators | An exception escapes the handler, a validator, or the save. |

The pipeline never throws to the caller. [ARCHITECTURE.md](ARCHITECTURE.md#exception-and-cancellation-handling) lists each exception, its result, and its log event.

## Validators

A validator implements `IValidator<TRequest>`. `AddCqrsHandlers` registers validators as scoped services, and a request can have several. The validation decorator runs before any transaction or database call:

1. If the request has no validators, it calls the next step.
2. Otherwise it passes one empty `ValidationState` to each validator in turn, so all errors are collected together.
3. If there are errors, it logs `RequestValidationFailed` (1007) and returns `Invalid` or `Invalid<T>`. No transaction starts, and neither the handler nor any hook runs.

`Validate` is synchronous, so checks that need the database belong in the handler instead.

### Open generic validators

A validator can be an open generic over the request type, with constraints that pick which requests it applies to. `AddCqrsHandlers` registers it as an open generic, so it runs for every request that meets its constraints, alongside that request's own validators. Each constraint must hold, so a validator constrained to two interfaces runs only for requests that implement both. From [`Listing/PagingValidators.cs`](../../DannyGoodacre.Cqrs.Examples/Listing/PagingValidators.cs):

```csharp
// The constraints must all hold, so this validator runs only for requests that are both paged and searchable.
// ListQuery<TItem> is paged but not searchable, so it does not get this validator.
public sealed class SearchPagingValidator<TRequest> : IValidator<TRequest>
    where TRequest : IRequest, IPagedRequest, ISearchableRequest
{
    public const int MaxSearchPage = 5;

    public void Validate(TRequest request, ValidationState validationState)
    {
        if (request.Page > MaxSearchPage)
        {
            validationState.AddError(nameof(IPagedRequest.Page), $"Search results stop after page {MaxSearchPage}.");
        }
    }
}
```

`IValidator<TRequest>` requires `TRequest : IRequest`, so an open validator repeats that constraint; with no other constraint, it runs for every request.

The validator must have one type parameter, and that parameter must be the request type, as in `SearchPagingValidator<TRequest> : IValidator<TRequest>`. It may get that interface from an abstract generic base class: `PagingValidator<TRequest>` in the same file derives from [`ValidatorBase<TRequest>`](../../DannyGoodacre.Cqrs.Examples/Validation/ValidatorBase.cs), and `SearchCustomersQueryValidator` in [`Customers/ListCustomers.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/ListCustomers.cs) is a closed validator derived from the same base.

`AddCqrsHandlers` throws `InvalidOperationException` at startup for any other non-abstract open generic validator, because it would never run: one whose type parameter is inside the request type, such as `OpenListQueryValidator<TItem> : IValidator<ListQuery<TItem>>`, one that validates a closed request type, or one with two type parameters. [`InvalidOpenGenericTypes.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/InvalidOpenGenericTypes.cs) in the example tests shows each case.

To share rules across a generic request such as `ListQuery<TItem>`, make the generic validator abstract and derive a closed validator for each request type. `ListQueryValidator<TItem>` in [`Listing/PagingValidators.cs`](../../DannyGoodacre.Cqrs.Examples/Listing/PagingValidators.cs) sets a page size limit for each list, and `ListCustomersQueryValidator` and `ListPlansQueryValidator` derive from it, one for each list.

A validator can inject other validators. The onboarding validator reuses the rules of its child command, so the outer command fails before the transaction starts. From [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs):

```csharp
public sealed class OnboardCustomerCommandValidator(IValidator<RegisterCustomerCommand> customerValidator)
    : IValidator<OnboardCustomerCommand>
{
    public void Validate(OnboardCustomerCommand request, ValidationState validationState)
    {
        // Reuse the rules of the child command, so the outer command fails before the transaction starts.
        customerValidator.Validate(new RegisterCustomerCommand(request.Name, request.Email), validationState);

        if (string.IsNullOrWhiteSpace(request.PlanCode))
        {
            validationState.AddError(nameof(request.PlanCode), "Plan code is required.");
        }
    }
}
```

## ICommand

`ICommand` is the base interface for all commands. On its own, it marks a command that does not save to your database. The pipeline logs and validates it, then runs the handler; it does not save, open a transaction, or allow post-commit hooks, so it needs no `IStateUnit` or `ITransactionUnit`.

```csharp
public sealed record ResendWelcomeEmailCommand(string Email, string Name) : ICommand;
```

## IStateCommand

`IStateCommand` marks a command that changes state. The handler changes tracked entities, and the state decorator saves them with `IStateUnit.SaveChangesAsync` once the handler returns a success. If the save throws, the decorator returns `InternalError` or `Canceled` instead. The handler itself never calls `SaveChangesAsync`. The example is `RegisterCustomerCommand`, shown in the [README quick start](../README.md#quick-start).

Because the save happens after the handler returns, values the database generates during the save are not available to the handler's result. Inside an `ITransactedStateCommand`, a later step can read them, because the child save has already written the rows.

> **Note:** With EF Core, a standalone state command is already atomic. `SaveChangesAsync` wraps all of its changes in one internal transaction, so they are committed together or not at all. Use `ITransactedStateCommand` only when you need several steps, such as child commands or more than one save, to commit or roll back together.

## IQuery<T>

`IQuery<T>` marks a read-only request that returns a `T`. The pipeline logs and validates it, then runs the handler; it never saves or opens a transaction.

From [`Customers/GetCustomerQuery.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/GetCustomerQuery.cs):

```csharp
public sealed record GetCustomerQuery(Guid PublicId) : IQuery<CustomerDetails>;

public sealed class GetCustomerQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerQuery, CustomerDetails>
{
    public async Task<IResult<CustomerDetails>> HandleAsync(
        GetCustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        CustomerDetails? customer = await customerRepository.FindDetailsAsync(query.PublicId, cancellationToken);

        return customer is null
            ? new NotFound<CustomerDetails>()
            : new Success<CustomerDetails>(customer);
    }
}
```

## ITransactedStateCommand

`ITransactedStateCommand` marks a command that runs inside one explicit database transaction. Use it for a workflow that calls child commands and queries: if one step fails, every step's changes roll back. The transaction decorator runs the handler and then `SaveChangesAsync` inside `ITransactionUnit.ExecuteInTransactionAsync`, which commits on success and rolls back on failure. After the commit it runs the deferred hooks of child commands, then its own.

Child state commands save inside the outer transaction rather than opening their own; [Nested transactions](PERSISTENCE.md#nested-transactions) explains how.

### Example: customer onboarding

`OnboardCustomerCommand` calls a child command and a child query, then adds a subscription. From [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs):

```csharp
public sealed record OnboardCustomerCommand(string Name, string Email, string PlanCode) : ITransactedStateCommand;

public sealed class OnboardCustomerCommandHandler(
    ICommandHandler<RegisterCustomerCommand, Guid> registerCustomerHandler,
    IQueryHandler<GetPlanQuery, PlanDetails> getPlanHandler,
    /* ... */)
    : ICommandHandler<OnboardCustomerCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        OnboardCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        // Step 1: a child state command. Its SaveChangesAsync call writes the customer
        // inside this command's transaction. It does not start or commit a transaction.
        IResult<Guid> registerResult = await registerCustomerHandler.HandleAsync(
            new RegisterCustomerCommand(command.Name, command.Email), cancellationToken);

        if (registerResult is not Success<Guid>(var customerPublicId))
        {
            return registerResult;
        }

        // Step 2: a child query. If the plan does not exist, the transaction rolls back the new customer.
        IResult<PlanDetails> planResult = await getPlanHandler.HandleAsync(
            new GetPlanQuery(command.PlanCode), cancellationToken);

        if (planResult is not Success<PlanDetails>(var plan))
        {
            return planResult.MapFailure<Guid>();
        }

        // Steps 3 and 4: add the subscription. The transaction decorator saves it before the commit.
        // ...

        return new Success<Guid>(customerPublicId);
    }
}
```

The container injects the full pipeline for each child handler, so each child request is also logged and validated.

### Return a child result

The handler returns the first failure it receives. How depends on the result types:

| Orchestrator returns | Child returns | Action |
| --- | --- | --- |
| `IResult` | `IResult` or `IResult<T>` | Return the child result. `IResult<T>` is also an `IResult`. |
| `IResult<T>` | `IResult<T>` with the same `T` | Return the child result. |
| `IResult<TOut>` | `IResult<TIn>` | Return `childResult.MapFailure<TOut>()`. |

In the onboarding handler, a duplicate email reaches the caller as the child's `Conflict<Guid>`, and a missing plan as `NotFound<Guid>`. Either way the transaction decorator sees the failure and rolls back, without the handler knowing about the transaction.
