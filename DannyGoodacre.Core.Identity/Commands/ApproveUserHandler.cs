using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Identity.Commands;

internal sealed class ApproveUserHandler(ILogger <ApproveUserHandler> logger, UserManager<ApplicationUser> userManager)
    : CommandHandler<ApproveUserRequest>(logger), IApproveUser
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

        await userManager.AddToRoleAsync(user, "User");

        await userManager.UpdateAsync(user);

        return Result.Success();
    }

    public Task<Result> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ApproveUserRequest
        {
            UserId = userId
        }, cancellationToken);
}

internal sealed record ApproveUserRequest : ICommand
{
    public required string UserId { get; init; }
}

internal interface IApproveUser
{
    Task<Result> ExecuteAsync(string userId, CancellationToken cancellationToken = default);
}
