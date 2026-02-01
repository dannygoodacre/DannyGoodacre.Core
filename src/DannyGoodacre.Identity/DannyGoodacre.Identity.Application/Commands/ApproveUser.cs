using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Extensions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IApproveUser
{
    Task<Result> ExecuteAsync(string username, CancellationToken cancellationToken = default);
}

internal sealed record ApproveUserCommand : ICommand
{
    public required string Username { get; init; }
}

internal sealed class ApproveUserHandler(ILogger<ApproveUserHandler> logger,
                                         IUnitOfWork unitOfWork,
                                         IUserRepository repository)
    : PersistenceCommandHandler<ApproveUserCommand>(logger, unitOfWork), IApproveUser
{
    protected override string CommandName => "Approve User";

    protected override void Validate(ValidationState validationState, ApproveUserCommand command)
        => validationState.IsNotNullEmptyOrWhitespace(command.Username, nameof(command.Username));

    protected async override Task<Result> InternalExecuteAsync(ApproveUserCommand command,
                                                               CancellationToken cancellationToken = default)
    {
        if (!await repository.ExistsAsync(command.Username, cancellationToken))
        {
            return Result.NotFound();
        }

        await repository.ApproveAsync(command.Username, cancellationToken);

        return Result.Success();
    }

    public Task<Result> ExecuteAsync(string username, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ApproveUserCommand
        {
            Username = username
        }, cancellationToken);
}
