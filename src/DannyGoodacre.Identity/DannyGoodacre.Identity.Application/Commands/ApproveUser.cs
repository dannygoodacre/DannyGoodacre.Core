using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IApproveUser
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed record ApproveUserCommand : ICommand
{
    public required Guid Id { get; init; }
}

internal sealed class ApproveUserHandler(ILogger<ApproveUserHandler> logger,
                                         IStateUnit stateUnit,
                                         IUserRepository repository)
    : StateCommandHandler<ApproveUserCommand>(logger, stateUnit), IApproveUser
{
    protected override string CommandName => "Approve User";

    protected override void Validate(ValidationState validationState, ApproveUserCommand command)
        => validationState.IsNonEmptyGuid(command.Id, nameof(command.Id));

    protected async override Task<Result> InternalExecuteAsync(ApproveUserCommand command,
                                                               CancellationToken cancellationToken = default)
    {
        User? user = await repository.GetWithTrackingAsync(command.Id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.IsApproved = true;

        return Success();
    }

    public Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ApproveUserCommand
        {
            Id = id
        }, cancellationToken);
}
