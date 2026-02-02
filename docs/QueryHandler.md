# QueryHandler

Aside from the output type parameter `TResult` and the returned `Result<TResult>`, the `QueryHandler` class is functionally identical to `CommandHandler`. 

This handler provides a semantic distinction for operations that purely retrieve data.

## Signature

```csharp
public abstract partial class QueryHandler<TQuery, TResult>(ILogger logger) where TQuery : IQuery
```

Please refer to the [CommandHandler documentation](./CommandHandler.md) for details on members, validation logic, and execution flow, as `QueryHandler` follows the same lifecycle with an analogous query marker interface `IQuery`.

## Usage

```csharp
record GetStuffByIdQuery : IQuery
{
    public required int Id { get; init; }
}

class GetStuffByIdHandler(ILogger<GetStuffByIdHandler> logger, IStuffRepository repository) 
    : QueryHandler<GetStuffByIdQuery, StuffInfo>(logger)
{
    protected override string CommandName => "Get Stuff By ID";

    protected override async Task<Result<StuffInfo>> InternalExecuteAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        var stuff = await repository.GetByIdAsync(query.Id)

        return stuff is null 
            ? Result.NotFound();
            : Result.Success(user) 
    }
}
```
