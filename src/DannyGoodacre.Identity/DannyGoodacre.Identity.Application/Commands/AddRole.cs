using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddRole
{
    Task<Result> ExecuteAsync(string name, List<Guid> claimIds, CancellationToken cancellationToken = default);
}

internal sealed record AddRoleCommand : ICommand
{
    public required string Name { get; init; }

    public required List<Guid> ClaimIds { get; init; }
}

internal sealed class AddRoleHandler(ILogger<AddRoleHandler> logger,
                                     IStateUnit stateUnit,
                                     IRoleRepository roleRepository,
                                     IClaimRepository claimRepository)
    : StateCommandHandler<AddRoleCommand>(logger, stateUnit), IAddRole
{

    protected override string CommandName => "Add Role";

    protected override void Validate(ValidationState validationState, AddRoleCommand command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Name, nameof(command.Name));

        foreach (Guid claimId in command.ClaimIds)
        {
            validationState.IsNonEmptyGuid(claimId, nameof(command.ClaimIds));
        }
    }

protected async override Task<Result> InternalExecuteAsync(AddRoleCommand command, CancellationToken cancellationToken = default)
{
    if (await roleRepository.ExistsAsync(command.Name, cancellationToken))
    {
        return Conflict("Role already exists");
    }

    Dictionary<Guid, int> claimIdMap = await claimRepository.GetIdMapAsync(command.ClaimIds, cancellationToken);

    List<Guid> missingClaimIds = command.ClaimIds
        .Where(id => !claimIdMap.ContainsKey(id))
        .ToList();

    if (missingClaimIds.Count > 0)
    {
        return DomainError($"Missing claims: {string.Join(' ', missingClaimIds)}.");
    }

    roleRepository.Add(new Role
    {
        PublicId = Guid.NewGuid(),
        Name = command.Name,
        Claims = command.ClaimIds.Select(x => new RoleClaim
        {
            ClaimId = claimIdMap[x]
        }).ToList()
    });

    return Success();
}


    public Task<Result> ExecuteAsync(string name, List<Guid> claimIds, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddRoleCommand
        {
            Name = name,
            ClaimIds = claimIds
        }, cancellationToken);
}
