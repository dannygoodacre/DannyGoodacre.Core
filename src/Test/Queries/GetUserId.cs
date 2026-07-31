using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using Test.Repositories;

namespace Test.Queries;

internal interface IGetUserId
{
    Task<Result<int>> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

internal sealed record GetUserIdQuery : IQuery
{
    public required string Name { get; init; }
}

internal sealed class GetUserIdHandler(ILogger<GetUserIdHandler> logger, IUserRepository repository)
    : QueryHandler<GetUserIdQuery, int>(logger), IGetUserId
{

    protected override string QueryName => "Get User ID";

    protected async override Task<Result<int>> InternalExecuteAsync(GetUserIdQuery query, CancellationToken cancellationToken = default)
    {
        User? user = await repository.GetAsync(query.Name, cancellationToken);

        return user is null
            ? NotFound()
            : Success(user.Id);
    }

    public Task<Result<int>> ExecuteAsync(string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(new GetUserIdQuery
        {
            Name = name
        }, cancellationToken);
}
