using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IApproveUser
{
    Task<Result> ExecuteAsync(string userId, CancellationToken cancellationToken = default);
}

internal sealed record ApproveUserCommand : ICommand
{
    public required string UserId { get; init; }
}

internal sealed class ApproveUserHandler(ILogger <ApproveUserHandler> logger,
                                         IUnitOfWork unitOfWork,
                                         IUserManager<IdentityUser> userManager)
    : TransactionCommandHandler<ApproveUserCommand>(logger, unitOfWork), IApproveUser
{

    protected override string CommandName => "Approve User";

    protected override void Validate(ValidationState validationState, ApproveUserCommand command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.UserId, nameof(command.UserId));
    }

    protected async override Task<Result> InternalExecuteAsync(ApproveUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);

        if (user is null)
        {
            return Result.NotFound();
        }

        user.EmailConfirmed = true;

        var result = await userManager.AddToRoleAsync(user, "User");

        if (!result.IsSuccess)
        {
            return result;
        }

        return await userManager.UpdateAsync(user);
    }

    public Task<Result> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ApproveUserCommand
        {
            UserId = userId
        }, cancellationToken);
}
