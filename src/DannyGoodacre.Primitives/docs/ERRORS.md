# Errors

This document describes the `Error` struct. `InternalError` and `InternalError<T>` use it to describe an unexpected fault.

## Error

`Error` is a `readonly record struct`. It has three properties:

| Property | Type | Description |
| --- | --- | --- |
| `Message` | `string` | A description of the error for people. |
| `Code` | `string` | A category or code for software. The default is `"General"`. |
| `Exception` | `Exception?` | The exception that caused the error, or `null`. JSON serialization ignores this property. |

## Create an error

| Constructor | `Message` | `Code` | `Exception` |
| --- | --- | --- | --- |
| `Error(string message)` | `message` | `"General"` | `null` |
| `Error(string message, string code)` | `message` | `code` | `null` |
| `Error(Exception exception)` | `exception.Message` | The exception type name, for example `"TimeoutException"` | `exception` |
| `Error(Exception exception, string code)` | `exception.Message` | `code` | `exception` |
| `Error(string message, string code = "General", Exception? exception = null)` | `message` | `code` | `exception` |

The constructors replace empty values:

- If `message` is `null`, empty, or white space, `Message` is `"An unspecified error occurred."`.
- If `code` is `null`, empty, or white space, `Code` is `"General"`.

`default(Error)` and `new Error()` do not run a constructor. They still return the same default `Message` and `Code`, and they equal `new Error("")`.

The constructors that take an `Exception` throw `ArgumentNullException` if the exception is `null`.

### Implicit conversions

A `string` or an `Exception` converts to an `Error` with no cast.

```csharp
Error fromMessage = "The payment gateway did not respond.";   // Code is "General".
Error fromException = new TimeoutException("Gateway timeout."); // Code is "TimeoutException".
```

## Use an error with InternalError

The `InternalError` factory methods accept an `Error`, a `string`, or an `Exception`.

```csharp
using DannyGoodacre.Primitives;

namespace Shop.Payments;

public sealed record PaymentReceipt(string TransactionId, decimal Amount);

public interface IPaymentGateway
{
    Task<PaymentReceipt> ChargeAsync(string cardToken, decimal amount, CancellationToken cancellationToken);
}

public sealed class PaymentService(IPaymentGateway gateway)
{
    public async Task<IResult<PaymentReceipt>> ChargeAsync(
        string cardToken,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return Result<PaymentReceipt>.DomainError("The payment amount must be greater than zero.");
        }

        try
        {
            PaymentReceipt receipt = await gateway.ChargeAsync(cardToken, amount, cancellationToken);

            return Result.Success(receipt);
        }
        catch (OperationCanceledException)
        {
            return Result<PaymentReceipt>.Canceled();
        }
        catch (HttpRequestException ex)
        {
            return Result<PaymentReceipt>.InternalError(new Error(ex, "PAYMENT_GATEWAY_UNAVAILABLE"));
        }
    }
}
```

This service is at a system boundary. It catches the gateway exceptions and converts them to results.

> **Note:** In a `DannyGoodacre.Cqrs` handler, do not catch exceptions to create `InternalError`. The pipeline does this for you.

## Read an error

```csharp
IResult<PaymentReceipt> result = await paymentService.ChargeAsync(cardToken, 49.99m, cancellationToken);

if (result is InternalError(var error))
{
    logger.LogError(error.Exception, "Payment failed with code {Code}: {Message}", error.Code, error.Message);
}
```

Use `Code` to select an action in software. Use `Message` for logs and support staff.
Do not show `Message` to end users. It can contain internal details from the exception.

## Text output

| Condition | `ToString()` output |
| --- | --- |
| `Code` is `"General"` | `<Message>` |
| `Code` is a different value | `[<Code>] <Message>` |

Examples:

- `new Error("Database timeout").ToString()` returns `Database timeout`.
- `new Error("Access denied", "AUTH_001").ToString()` returns `[AUTH_001] Access denied`.

## Equality

`Error` is a record struct. Two errors are equal when `Message`, `Code`, and `Exception` are equal. `Exception` is compared by reference.

## Related documents

- [README.md](../README.md)
- [RESULTS.md](RESULTS.md)
- [VALIDATION.md](VALIDATION.md)
