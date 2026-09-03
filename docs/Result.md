# Result

The `IResult` and `IResult<T>` interfaces provide a unified way to represent operation outcomes. Instead of relying on exceptions for flow control, these records encapsulate success, failure, and validation states in a predictable, type-safe manner.

Using pattern matching, we can safely convert a given `IResult` to its outcome. 

## Outcomes

| Outcome       | Definition                                            |
|---------------|-------------------------------------------------------|
| Success       | The operation completed normally                      |
| Canceled      | The request was canceled by the caller                |
| Conflict      | The request conflicts with the current resource state |
| DomainError   | A business logic rule was violated                    |
| InternalError | An unexpected system failure or exception occurred    |
| Invalid       | The request failed validation rules                   |
| NotFound      | The requested resource does not exist                 |

## IResult

This inteface represents the status of an operation only.

### Example

```csharp
public IResult DeleteUser(int id)
{
    var user = _repository.Find(id);

    if (user is null)
    {
        return Result.NotFound();
    }

    _repository.Delete(user);

    return Result.Success();
}
    
// Consuming the result
IResult result = DeleteUser(123);

if (result is Success)
{
    ...
}
else if (result is Canceled)
{
    ...
}
```

## IResult\<T\>

The generic `IResult<T>` adds a `Value` property of type `T`.

Since `IResult<T>` inherits from `IResult`, checking for non-success outcomes is simplified: we may check that an `IResult<T>` is an instance of `Conflict` instead of `Conflict<T>`, etc.

### Example

```csharp
public IResult<UserResponse> GetUser(int id)
{
    User user = _repository.Find(id);

    if (user is null)
    {
        return Result<UserResponse>.NotFound();
    }
    
    var userResponse = new UserResponse(user.Name);

    return Result.Success(userResponse);
}

// Consuming the result
IResult<UserResponse> result = service.GetUser(userId);

if (result is Success<UserResponse>(var value))
{
    Console.WriteLine(value.Name);
}
else if (result is Conflict conflict)
{
    Console.WriteLine(conflict.Message);
}
```
