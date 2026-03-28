# Result

The `Result` and `Result<T>` classes provide a unified way to handle operation outcomes. Instead of relying on exceptions for flow control, these classes encapsulate success, failure, and validation states in a predictable, type-safe manner.

## Result

This class represents the status of an operation only. It tracks the status of the operation and, where applicable, a [ValidationState](./ValidationState.md) object and/or an `Exception` object.

### Example

```csharp
public Result DeleteUser(int id)
{
    var user = _repository.Find(id);

    if (user is null)
    {
        return Result.NotFound();
    }

    _repository.Delete(user);

    return Result.Success();
}
```

## Result\<T\>

Inheriting from `Result`, the generic `Result<T>` adds a `Value` property of type `T`.

### Example

```csharp
public Result<UserDto> GetUser(int id)
{
    var user = _repository.Find(id);

    if (user is null)
    {
        return Result<UserDto>.NotFound();
    }

    return Result.Success(new UserDto(user.Name));
}

// Consuming the result
var result = service.GetUser(userId);

if (result.IsSuccess)
{
    Console.WriteLine(result.Value.Name);
}
```

## Status

These classes support various operational outcomes via the `Status` enum:

| Status | Definition |
| --- | --- |
| Success | Operation completed normally |
| Invalid | Input failed validation rules (carries a `ValidationState`) |
| DomainError | A business logic rule was violated |
| Canceled | The request was canceled by the caller |
| NotFound | The requested resource does not exist |
| InternalError | An unexpected system failure or exception occurred |
