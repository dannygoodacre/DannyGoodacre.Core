# Commands and queries

This document tells you how to select a request marker and how to write each type of request.
It also shows xUnit tests for each pipeline.

## Select a marker interface

| Marker | Saves data | Transaction | Post-commit hook | Use for |
| --- | --- | --- | --- | --- |
| `ICommand` | No | No | Does not run | Actions with no database change, for example a call to an external API. |
| `IStateCommand` | Yes. The pipeline calls `SaveChangesAsync`. | No new transaction. Uses the outer transaction if one is open. | Runs after the save | One change to one aggregate. |
| `ITransactedStateCommand` | Yes. The pipeline saves before the commit. | Yes. One explicit transaction. | Runs after the commit | Workflows that run many child commands and queries as one unit. |
| `IQuery<T>` | No | No | Not available | Read operations that return data. |

Use these rules:

1. If the request returns data and does not change state, use `IQuery<T>`.
2. If the request changes data in one step, use `IStateCommand`.
3. If the request runs more than one state-changing step, use `ITransactedStateCommand`.
4. If the request changes no data in your database, use `ICommand`.

> **Note:** A type must not implement both `IQuery<T>` and `IStateCommand` or `ITransactedStateCommand`. `AddCqrsHandlers` throws `InvalidOperationException` at startup for this type.

> **Note:** If a command implements both `IStateCommand` and `ITransactedStateCommand`, the pipeline uses the `ITransactedStateCommand` behavior.

## Handler interfaces

| Request | Handler interface | Return type |
| --- | --- | --- |
| Command with no return value | `ICommandHandler<TCommand>` | `Task<IResult>` |
| Command with a return value | `ICommandHandler<TCommand, TResult>` | `Task<IResult<TResult>>` |
| Query | `IQueryHandler<TQuery, TResult>` | `Task<IResult<TResult>>` |

`TResult` must not be nullable. To show that data is missing, return `NotFound`. Do not return `null`.

## Shared domain model

The examples in this document use this order processing model.

```csharp
namespace Shop.Orders;

public enum OrderStatus
{
    Placed,
    Canceled
}

public sealed class Product
{
    public required string Sku { get; init; }

    public required decimal UnitPrice { get; init; }

    public int StockLevel { get; set; }
}

public sealed class Order(Guid id, Guid customerId, string productSku, int quantity, decimal total)
{
    public Guid Id { get; private set; } = id;

    public Guid CustomerId { get; private set; } = customerId;

    public string ProductSku { get; private set; } = productSku;

    public int Quantity { get; private set; } = quantity;

    public decimal Total { get; private set; } = total;

    public OrderStatus Status { get; set; } = OrderStatus.Placed;
}

public sealed record OrderSummary(Guid Id, Guid CustomerId, string ProductSku, int Quantity, decimal Total, OrderStatus Status);

public interface IProductRepository
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    void Add(Order order);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrderSummary?> FindSummaryAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task SendOrderConfirmationAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);

    Task SendOrderCancellationAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string email, CancellationToken cancellationToken = default);
}
```

[PERSISTENCE.md](PERSISTENCE.md) shows EF Core implementations of the repositories.

## ICommand

`ICommand` is the base interface for all commands. Use it alone for commands that do not save data to your database.

The pipeline for an `ICommand`:

- Logs the request.
- Runs the validator, if there is one.
- Runs the handler.
- Does not call `SaveChangesAsync`.
- Does not open a transaction.
- Does not run post-commit hooks.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Accounts;

public sealed record SendPasswordResetEmailCommand(string Email) : ICommand;

public sealed class SendPasswordResetEmailCommandValidator : IValidator<SendPasswordResetEmailCommand>
{
    public void Validate(SendPasswordResetEmailCommand request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            validationState.AddError(nameof(request.Email), "A valid email address is required.");
        }
    }
}

public sealed class SendPasswordResetEmailCommandHandler(IEmailSender emailSender)
    : ICommandHandler<SendPasswordResetEmailCommand>
{
    public async Task<IResult> HandleAsync(
        SendPasswordResetEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        await emailSender.SendPasswordResetAsync(command.Email, cancellationToken);

        return Result.Success();
    }
}
```

## IStateCommand

`IStateCommand` marks a command that changes state.
The handler changes tracked entities. The pipeline saves them.

The state decorator does these steps:

1. It calls the handler.
2. If the result is a failure, it returns the result. It does not save.
3. It calls `IStateUnit.SaveChangesAsync`.
4. If the save fails, it returns `InternalError` or `Canceled`. It does not run the hook.
5. It runs the post-commit hook, if there is one.
6. It returns the handler result.

Do not call `SaveChangesAsync` in an `IStateCommand` handler.

### Command with a return value

[README.md](../README.md#quick-start) shows `PlaceOrderCommand`. The command returns the new order ID.

### Command with no return value

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed record CancelOrderCommand(Guid OrderId) : IStateCommand;

public sealed class CancelOrderCommandValidator : IValidator<CancelOrderCommand>
{
    public void Validate(CancelOrderCommand request, ValidationState validationState)
    {
        if (request.OrderId == Guid.Empty)
        {
            validationState.AddError(nameof(request.OrderId), "Order ID is required.");
        }
    }
}

public sealed class CancelOrderCommandHandler(IOrderRepository orderRepository)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<IResult> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        Order? order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.NotFound();
        }

        if (order.Status == OrderStatus.Canceled)
        {
            return Result.DomainError("The order is already canceled.");
        }

        order.Status = OrderStatus.Canceled;

        return Result.Success();
    }
}

public sealed class SendOrderCancellationHook(IEmailSender emailSender)
    : IPostCommitHook<CancelOrderCommand>
{
    public Task ExecuteAsync(CancelOrderCommand command, IResult result, CancellationToken cancellationToken = default)
        => emailSender.SendOrderCancellationAsync(command.OrderId, cancellationToken);
}
```

### Entity keys

The handler creates its result before the pipeline saves the data.
Thus, a key that the database generates during the save is not in the result.

Generate keys in the handler. Use `Guid.CreateVersion7()` or a similar client-side generator.
Then the handler knows the key and can return it in `Success<T>`.

## ITransactedStateCommand

`ITransactedStateCommand` marks a command that runs inside one explicit database transaction.
Use it for a workflow that calls child commands and queries.
If one step fails, all changes from all steps roll back.

The transaction decorator does these steps:

1. It calls `ITransactionUnit.ExecuteInTransactionAsync`. The transaction unit opens a transaction.
2. Inside the transaction, it calls the handler.
3. If the result is a failure, it returns the result. The transaction unit rolls back.
4. If the result is a success, it calls `SaveChangesAsync`. The transaction unit commits.
5. After the commit, it runs the post-commit hook, if there is one.
6. It returns the handler result.

### Example: customer onboarding

The workflow creates a customer, reads an email template, and schedules a welcome email.
The three steps save data as one unit.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Customers;

// Data access contracts.
public sealed record Customer(Guid Id, string Name, string Email);

public sealed record ScheduledEmail(Guid Id, Guid CustomerId, string Body);

public interface ICustomerRepository
{
    void Add(Customer customer);
}

public interface IEmailTemplateRepository
{
    Task<string?> FindWelcomeTemplateAsync(string languageCode, CancellationToken cancellationToken = default);
}

public interface IScheduledEmailRepository
{
    void Add(ScheduledEmail email);
}

// Child requests.
public sealed record CreateCustomerCommand(string Name, string Email) : IStateCommand;

public sealed record GetWelcomeEmailTemplateQuery(string LanguageCode) : IQuery<string>;

public sealed record ScheduleEmailCommand(Guid CustomerId, string EmailBody) : IStateCommand;

// Orchestrator request.
public sealed record OnboardCustomerCommand(string Name, string Email, string LanguageCode) : ITransactedStateCommand;

// Child handlers.
public sealed class CreateCustomerCommandHandler(ICustomerRepository customerRepository)
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    public Task<IResult<Guid>> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = new Customer(Guid.CreateVersion7(), command.Name, command.Email);

        customerRepository.Add(customer);

        return Task.FromResult<IResult<Guid>>(Result<Guid>.Success(customer.Id));
    }
}

public sealed class GetWelcomeEmailTemplateQueryHandler(IEmailTemplateRepository templateRepository)
    : IQueryHandler<GetWelcomeEmailTemplateQuery, string>
{
    public async Task<IResult<string>> HandleAsync(
        GetWelcomeEmailTemplateQuery query,
        CancellationToken cancellationToken = default)
    {
        string? template = await templateRepository.FindWelcomeTemplateAsync(query.LanguageCode, cancellationToken);

        return template is null
            ? Result<string>.NotFound()
            : Result<string>.Success(template);
    }
}

public sealed class ScheduleEmailCommandHandler(IScheduledEmailRepository scheduledEmailRepository)
    : ICommandHandler<ScheduleEmailCommand>
{
    public Task<IResult> HandleAsync(ScheduleEmailCommand command, CancellationToken cancellationToken = default)
    {
        scheduledEmailRepository.Add(new ScheduledEmail(Guid.CreateVersion7(), command.CustomerId, command.EmailBody));

        return Task.FromResult<IResult>(Result.Success());
    }
}

// Orchestrator handler.
public sealed class OnboardCustomerCommandHandler(
    ICommandHandler<CreateCustomerCommand, Guid> createCustomerHandler,
    IQueryHandler<GetWelcomeEmailTemplateQuery, string> getTemplateHandler,
    ICommandHandler<ScheduleEmailCommand> scheduleEmailHandler)
    : ICommandHandler<OnboardCustomerCommand>
{
    public async Task<IResult> HandleAsync(OnboardCustomerCommand command, CancellationToken cancellationToken = default)
    {
        IResult<Guid> createResult = await createCustomerHandler.HandleAsync(
            new CreateCustomerCommand(command.Name, command.Email), cancellationToken);

        if (createResult is not Success<Guid>(var customerId))
        {
            return createResult;
        }

        IResult<string> templateResult = await getTemplateHandler.HandleAsync(
            new GetWelcomeEmailTemplateQuery(command.LanguageCode), cancellationToken);

        if (templateResult is not Success<string>(var template))
        {
            return templateResult;
        }

        IResult scheduleResult = await scheduleEmailHandler.HandleAsync(
            new ScheduleEmailCommand(customerId, template), cancellationToken);

        if (scheduleResult.IsFailure)
        {
            return scheduleResult;
        }

        return Result.Success();
    }
}
```

The orchestrator returns the first failure result. The transaction decorator then rolls back the transaction.
`IResult<T>` is also an `IResult`, so the orchestrator can return a child result directly.

The container injects the full pipeline for each child handler.
Thus, each child request is also logged and validated.

### Why IStateCommand is safe inside ITransactedStateCommand

A child `IStateCommand` does not create a nested transaction. These facts make this safe:

1. **The state decorator does not open transactions.** It only calls `IStateUnit.SaveChangesAsync`.
2. **One scope, one unit, one connection.** `IStateUnit` and `ITransactionUnit` resolve to the same scoped instance. That instance uses one `DbContext`. The outer transaction is open on that `DbContext`.
3. **EF Core uses the current transaction.** When a transaction is open, `SaveChangesAsync` writes inside it. It does not start or commit its own transaction.
4. **The save writes, but does not commit.** The rows go to the database inside the outer transaction. The database applies its constraints. Later steps can read the rows.
5. **Only the outermost unit commits.** The transaction decorator commits after all steps succeed. If a step fails, the rollback also removes the rows from the earlier child saves.

If a child command is also an `ITransactedStateCommand`, its transaction decorator calls `ExecuteInTransactionAsync` again.
`DbContextTransactionUnit` sees the open transaction and does not start a second one. See [PERSISTENCE.md](PERSISTENCE.md#nested-transactions).

> **Warning:** A post-commit hook on a child `IStateCommand` runs after the child save. In a transaction, this occurs before the outer commit. If the outer transaction rolls back later, the hook has already run. Attach post-commit hooks to the outermost command only.

## IQuery<T>

`IQuery<T>` marks a read-only request that returns a value of type `T`.

The pipeline for a query:

- Logs the request.
- Runs the validator, if there is one.
- Runs the handler.
- Does not save data or open transactions.

A query handler must not change state. Read data with no change tracking.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderSummary>;

public sealed class GetOrderByIdQueryValidator : IValidator<GetOrderByIdQuery>
{
    public void Validate(GetOrderByIdQuery request, ValidationState validationState)
    {
        if (request.OrderId == Guid.Empty)
        {
            validationState.AddError(nameof(request.OrderId), "Order ID is required.");
        }
    }
}

public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderSummary>
{
    public async Task<IResult<OrderSummary>> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        OrderSummary? summary = await orderRepository.FindSummaryAsync(query.OrderId, cancellationToken);

        return summary is null
            ? Result<OrderSummary>.NotFound()
            : Result<OrderSummary>.Success(summary);
    }
}
```

## IPostCommitHook

A post-commit hook runs a side effect after a state command saves its data.
Use hooks for actions outside the database, for example emails, messages, or cache updates.

| Command handler | Hook interface | Hook method |
| --- | --- | --- |
| `ICommandHandler<TCommand>` | `IPostCommitHook<TCommand>` | `ExecuteAsync(TCommand, IResult, CancellationToken)` |
| `ICommandHandler<TCommand, TResult>` | `IPostCommitHook<TCommand, TResult>` | `AfterCommitAsync(TCommand, IResult<TResult>, CancellationToken)` |

`AddCqrsHandlers` finds and registers hooks for you. You do not register them manually.

### How the pipeline delays the hook

The hook is a dependency of the persistence decorator, not of the handler.
The decorator calls the hook only at the end of the persistence step:

- **`IStateCommand`:** The state decorator calls the hook after `SaveChangesAsync` completes.
- **`ITransactedStateCommand`:** The transaction decorator calls the hook after `ExecuteInTransactionAsync` returns a success. At that time, the transaction is committed.

The hook does not run in these conditions:

- The validator finds errors.
- The handler returns a failure result.
- `SaveChangesAsync` or the transaction throws an exception, or is canceled.
- The command is a plain `ICommand`.
- `IStateCommand` only: the cancellation token is canceled after the save and before the hook starts. The pipeline logs event 3001 and returns the success result.

### Hook failures

The data is already saved when the hook runs.
If the hook is canceled, the pipeline returns the handler result. If the hook throws a different exception, the pipeline returns `InternalError`.
[ARCHITECTURE.md](ARCHITECTURE.md#exception-and-cancellation-handling) lists the log events.

> **Warning:** An `InternalError` from a hook does not mean that the save failed. The data is committed. Do not send the command again because of this result. Make hooks safe to fail, or catch and record their exceptions.

### Hook rules

- Register one hook for each command type. The pipeline resolves one hook. If you register more than one, the container uses the last registration.
- Keep the hook short. The caller waits for the hook to complete.
- For work that must not be lost, write an outbox record in the handler. Send the message from the outbox in a separate process.

## Tests

The tests use xUnit, Moq, and `DannyGoodacre.Testing`.
`DannyGoodacre.Testing` supplies the `ShouldBeSuccess`, `ShouldBeInvalid`, `ShouldBeNotFound`, and `ShouldBeDomainError` assertions.

The pipeline tests build a real service container with `AddCqrsHandlers`.
They test the handler and all decorators together.

### Command pipeline tests

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shop.Orders;
using Xunit;

namespace Shop.Tests.Orders;

public sealed class PlaceOrderPipelineTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new(MockBehavior.Strict);

    private readonly Mock<IOrderRepository> _orderRepositoryMock = new(MockBehavior.Strict);

    private readonly Mock<IEmailSender> _emailSenderMock = new(MockBehavior.Strict);

    private readonly Mock<ITransactionUnit> _transactionUnitMock = new(MockBehavior.Strict);

    private ICommandHandler<PlaceOrderCommand, Guid> BuildPipeline()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(_productRepositoryMock.Object);
        services.AddSingleton(_orderRepositoryMock.Object);
        services.AddSingleton(_emailSenderMock.Object);
        services.AddSingleton(_transactionUnitMock.Object);
        services.AddSingleton<IStateUnit>(_transactionUnitMock.Object);

        services.AddCqrsHandlers(typeof(PlaceOrderCommand).Assembly);

        IServiceScope scope = services.BuildServiceProvider().CreateScope();

        return scope.ServiceProvider.GetRequiredService<ICommandHandler<PlaceOrderCommand, Guid>>();
    }

    [Fact]
    public async Task HandleAsync_WhenStockIsAvailable_SavesOrderAndSendsConfirmation()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var command = new PlaceOrderCommand(Guid.NewGuid(), "SKU-100", 2);

        var product = new Product { Sku = "SKU-100", UnitPrice = 12.50m, StockLevel = 5 };

        _productRepositoryMock
            .Setup(x => x.GetBySkuAsync(command.ProductSku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _orderRepositoryMock
            .Setup(x => x.Add(It.Is<Order>(o => o.CustomerId == command.CustomerId && o.Total == 25.00m)));

        _transactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _emailSenderMock
            .Setup(x => x.SendOrderConfirmationAsync(command.CustomerId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        Guid orderId = result.ShouldBeSuccess();

        Assert.NotEqual(Guid.Empty, orderId);
        Assert.Equal(3, product.StockLevel);

        _transactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _emailSenderMock.Verify(
            x => x.SendOrderConfirmationAsync(command.CustomerId, orderId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenStockIsInsufficient_ReturnsDomainErrorAndDoesNotSave()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var command = new PlaceOrderCommand(Guid.NewGuid(), "SKU-100", 10);

        _productRepositoryMock
            .Setup(x => x.GetBySkuAsync(command.ProductSku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Sku = "SKU-100", UnitPrice = 12.50m, StockLevel = 5 });

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeDomainError("Insufficient stock.");

        _orderRepositoryMock.Verify(x => x.Add(It.IsAny<Order>()), Times.Never);

        _transactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _emailSenderMock.Verify(
            x => x.SendOrderConfirmationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShortCircuitsBeforeHandlerSaveAndHook()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var command = new PlaceOrderCommand(Guid.Empty, "", 0);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal(3, validationState.Errors.Count);
        Assert.Contains(nameof(PlaceOrderCommand.CustomerId), validationState.Errors.Keys);
        Assert.Contains(nameof(PlaceOrderCommand.ProductSku), validationState.Errors.Keys);
        Assert.Contains(nameof(PlaceOrderCommand.Quantity), validationState.Errors.Keys);

        _productRepositoryMock.Verify(
            x => x.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _transactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

A `Strict` mock throws an exception if the pipeline calls a member with no setup.
The logging decorator converts this exception to `InternalError`. The result assertion then fails.
Thus, the failure tests also prove that no unexpected database calls occur.

### Query pipeline tests

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shop.Orders;
using Xunit;

namespace Shop.Tests.Orders;

public sealed class GetOrderByIdPipelineTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new(MockBehavior.Strict);

    private IQueryHandler<GetOrderByIdQuery, OrderSummary> BuildPipeline()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(_orderRepositoryMock.Object);

        services.AddCqrsHandlers(typeof(GetOrderByIdQuery).Assembly);

        IServiceScope scope = services.BuildServiceProvider().CreateScope();

        return scope.ServiceProvider.GetRequiredService<IQueryHandler<GetOrderByIdQuery, OrderSummary>>();
    }

    [Fact]
    public async Task HandleAsync_WhenOrderExists_ReturnsSummary()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var expected = new OrderSummary(Guid.NewGuid(), Guid.NewGuid(), "SKU-100", 2, 25.00m, OrderStatus.Placed);

        _orderRepositoryMock
            .Setup(x => x.FindSummaryAsync(expected.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        IResult<OrderSummary> result = await pipeline.HandleAsync(new GetOrderByIdQuery(expected.Id), CancellationToken.None);

        // Assert
        OrderSummary summary = result.ShouldBeSuccess();

        Assert.Equal(expected, summary);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.FindSummaryAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderSummary?)null);

        // Act
        IResult<OrderSummary> result = await pipeline.HandleAsync(new GetOrderByIdQuery(orderId), CancellationToken.None);

        // Assert
        result.ShouldBeNotFound();
    }

    [Fact]
    public async Task HandleAsync_WhenOrderIdIsEmpty_ShortCircuitsWithoutCallingRepository()
    {
        // Arrange
        var pipeline = BuildPipeline();

        // Act
        IResult<OrderSummary> result = await pipeline.HandleAsync(new GetOrderByIdQuery(Guid.Empty), CancellationToken.None);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Single(validationState.Errors);
        Assert.Contains(nameof(GetOrderByIdQuery.OrderId), validationState.Errors.Keys);

        _orderRepositoryMock.Verify(
            x => x.FindSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

The query container does not register `IStateUnit` or `ITransactionUnit`.
Query pipelines do not use them.

### Transacted command pipeline tests

The mock transaction unit runs the operation delegate directly.
The tests prove the call sequence. They do not prove the database rollback.
Use an integration test with a real database to prove the rollback.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shop.Customers;
using Xunit;

namespace Shop.Tests.Customers;

public sealed class OnboardCustomerPipelineTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new(MockBehavior.Strict);

    private readonly Mock<IEmailTemplateRepository> _templateRepositoryMock = new(MockBehavior.Strict);

    private readonly Mock<IScheduledEmailRepository> _scheduledEmailRepositoryMock = new(MockBehavior.Strict);

    private readonly Mock<ITransactionUnit> _transactionUnitMock = new(MockBehavior.Strict);

    public OnboardCustomerPipelineTests()
    {
        _transactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<IResult>> operation, CancellationToken ct) => operation(ct));

        _transactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private ICommandHandler<OnboardCustomerCommand> BuildPipeline()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(_customerRepositoryMock.Object);
        services.AddSingleton(_templateRepositoryMock.Object);
        services.AddSingleton(_scheduledEmailRepositoryMock.Object);
        services.AddSingleton(_transactionUnitMock.Object);
        services.AddSingleton<IStateUnit>(_transactionUnitMock.Object);

        services.AddCqrsHandlers(typeof(OnboardCustomerCommand).Assembly);

        IServiceScope scope = services.BuildServiceProvider().CreateScope();

        return scope.ServiceProvider.GetRequiredService<ICommandHandler<OnboardCustomerCommand>>();
    }

    [Fact]
    public async Task HandleAsync_WhenAllStepsSucceed_RunsAllStepsInOneTransaction()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "en-GB");

        _customerRepositoryMock.Setup(x => x.Add(It.Is<Customer>(c => c.Email == command.Email)));

        _templateRepositoryMock
            .Setup(x => x.FindWelcomeTemplateAsync("en-GB", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Welcome to the shop.");

        _scheduledEmailRepositoryMock.Setup(x => x.Add(It.Is<ScheduledEmail>(e => e.Body == "Welcome to the shop.")));

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _transactionUnitMock.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<IResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Two child state command saves and one final save, all in one transaction.
        _transactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task HandleAsync_WhenTemplateIsMissing_ReturnsNotFoundAndStopsBeforeLastStep()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "xx-XX");

        _customerRepositoryMock.Setup(x => x.Add(It.IsAny<Customer>()));

        _templateRepositoryMock
            .Setup(x => x.FindWelcomeTemplateAsync("xx-XX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeNotFound();

        _scheduledEmailRepositoryMock.Verify(x => x.Add(It.IsAny<ScheduledEmail>()), Times.Never);

        // Only the child customer save runs. The transaction unit rolls it back.
        _transactionUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Related documents

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [PERSISTENCE.md](PERSISTENCE.md)
- [RESULT-HANDLING.md](RESULT-HANDLING.md)
