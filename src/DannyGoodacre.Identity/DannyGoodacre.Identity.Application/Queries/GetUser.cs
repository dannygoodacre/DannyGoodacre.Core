using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

internal sealed record GetUserQuery : IQuery
{
    public required Guid Id { get; init; }
}

public interface IGetUser
{
    Task<Result<UserInfoResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class GetUserHandler(ILogger<GetUserHandler> logger, IUserRepository repository) : QueryHandler<GetUserQuery, UserInfoResponse>(logger), IGetUser
{
    protected override string QueryName => "Get User";

    protected override void Validate(ValidationState state, GetUserQuery query)
    {
        state.IsNonEmptyGuid(query.Id, nameof(query.Id));
    }

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        User? user = await repository.GetAsync(query.Id, cancellationToken);

        return user is null
            ? NotFound()
            : Success(user.ToUserInfoResponse());
    }

    public Task<Result<UserInfoResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new GetUserQuery
        {
            Id = id
        }, cancellationToken);
}
