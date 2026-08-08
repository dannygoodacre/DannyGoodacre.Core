using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddRole
{
    Task<Result> ExecuteAsync(AddRoleCommand command, CancellationToken cancellationToken = default);
}

public sealed record AddRoleCommand : ICommand
{
    public required string Name { get; init; }

    public required List<Guid> ClaimIds { get; init; }
}

internal sealed partial class AddRoleHandler(ILogger<AddRoleHandler> logger,
                                     IStateUnit stateUnit,
                                     IRoleRepository roleRepository,
                                     IClaimRepository claimRepository)
    : StateCommandHandler<AddRoleCommand>(logger, stateUnit), IAddRole
{
    protected override string CommandName => "Add Role";

    public new Task<Result> ExecuteAsync(AddRoleCommand command, CancellationToken cancellationToken = default)
        => base.ExecuteAsync(command, cancellationToken);

    protected override void Validate(ValidationState validationState, AddRoleCommand command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Name, nameof(command.Name));

        if (!validationState.IsNotNullOrEmpty(command.ClaimIds, nameof(command.ClaimIds)))
        {
            return;
        }

        foreach (Guid claimId in command.ClaimIds)
        {
            validationState.IsNonEmptyGuid(claimId, nameof(command.ClaimIds));
        }
    }

    protected async override Task<Result> InternalExecuteAsync(AddRoleCommand command, CancellationToken cancellationToken = default)
    {
        string formattedCommandClaimIds = string.Join(", ", command.ClaimIds);

        LogStarted(Logger, CommandName, command.Name, formattedCommandClaimIds);

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
            LogMissingClaims(Logger, CommandName, command.Name, string.Join(", ", missingClaimIds));

            return DomainError($"Missing claims: {string.Join(", ", missingClaimIds)}.");
        }

        _ = roleRepository.Add(new Role
        {
            PublicId = Guid.NewGuid(),
            Name = command.Name,
            Claims = command.ClaimIds.Select(claimId => new RoleClaim
            {
                ClaimId = claimIdMap[claimId]
            }).ToList()
        });

        LogCompleted(Logger, CommandName, command.Name, formattedCommandClaimIds);

        return Success();
    }

    [LoggerMessage(LogLevel.Information, "Command '{Command}' started for Role '{Role}' and Claim IDs '{ClaimIds}'.")]
    private static partial void LogStarted(ILogger logger, string command, string role, string claimIds);

    [LoggerMessage(LogLevel.Warning, "Command '{Command}' failed for Role '{Role}': missing Claims with IDs '{ClaimIds}'.")]
    private static partial void LogMissingClaims(ILogger logger, string command, string role, string claimIds);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' completed for Role '{Role}' and Claim IDs '{ClaimIds}'.")]
    private static partial void LogCompleted(ILogger logger, string command, string role, string claimIds);
}
