# Errors

`InternalError` carries an `Error`, a `readonly record struct` that describes an unexpected fault.

| Property | Type | Description |
| --- | --- | --- |
| `Message` | `string` | A description for people. |
| `Code` | `string` | A category or code for software. Defaults to `"General"`. |
| `Exception` | `Exception?` | The exception that caused the error, or `null`. Ignored by JSON serialization. |

| Constructor | `Message` | `Code` | `Exception` |
| --- | --- | --- | --- |
| `Error(string message)` | `message` | `"General"` | `null` |
| `Error(string message, string code)` | `message` | `code` | `null` |
| `Error(Exception exception)` | `exception.Message` | The exception type name, for example `"TimeoutException"` | `exception` |
| `Error(Exception exception, string code)` | `exception.Message` | `code` | `exception` |
| `Error(string message, string code = "General", Exception? exception = null)` | `message` | `code` | `exception` |

- A `null`, empty, or white-space `message` becomes `"An unspecified error occurred."`, and a `code` like that becomes `"General"`. `default(Error)` returns the same defaults.
- The `Exception` constructors throw `ArgumentNullException` for a `null` exception.
- A `string` or an `Exception` converts to an `Error` implicitly, so `new InternalError<T>("...")` and `new InternalError<T>(ex)` both work.

At a system boundary, convert exceptions from external services into results:

```csharp
catch (HttpRequestException ex)
{
    return new InternalError<PaymentReceipt>(new Error(ex, "PAYMENT_GATEWAY_UNAVAILABLE"));
}
```

In a `DannyGoodacre.Cqrs` handler this is unnecessary, because the pipeline converts exceptions for you.

Use `Code` to make decisions in software and `Message` for logs and support staff. `Message` can contain internal details from the exception, so it is not suitable for end users.

Two errors are equal when `Message`, `Code`, and `Exception` are equal. `Exception` is compared by reference.
