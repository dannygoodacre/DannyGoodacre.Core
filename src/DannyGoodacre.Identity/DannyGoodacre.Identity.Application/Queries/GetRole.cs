using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

public interface IGetRole
{
    Task<Result<RoleResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed record GetRoleQuery : IQuery
{
    public required Guid Id { get; init; }
}

internal sealed class GetRoleHandler(ILogger<GetRoleHandler> logger, IRoleRepository repository)
    : QueryHandler<GetRoleQuery, RoleResponse>(logger), IGetRole
{
    protected override string QueryName => "Get Role";

    protected async override Task<Result<RoleResponse>> InternalExecuteAsync(GetRoleQuery query, CancellationToken cancellationToken = default)
    {
        Role? role = await repository.GetAsync(query.Id, cancellationToken);

        return role is null
            ? NotFound()
            : Success(role.ToResponse());
    }

    public Task<Result<RoleResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new GetRoleQuery
        {
            Id = id
        }, cancellationToken);
}
