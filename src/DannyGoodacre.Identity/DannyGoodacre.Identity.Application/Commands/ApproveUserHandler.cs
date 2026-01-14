using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

internal sealed class ApproveUserHandler(ILogger <ApproveUserHandler> logger,
                                         IUnitOfWork unitOfWork,
                                         IUserManager<IdentityUser> userManager)
    : TransactionCommandHandler<ApproveUserRequest>(logger, unitOfWork), IApproveUser
{

    protected override string CommandName => "Approve User";

    protected async override Task<Result> InternalExecuteAsync(ApproveUserRequest command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);

        if (user is null)
        {
            return Result.DomainError("User not found");
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
        => ExecuteAsync(new ApproveUserRequest
        {
            UserId = userId
        }, cancellationToken);
}

internal sealed record ApproveUserRequest : ICommandRequest
{
    public required string UserId { get; init; }
}

public interface IApproveUser
{
    Task<Result> ExecuteAsync(string userId, CancellationToken cancellationToken = default);
}
