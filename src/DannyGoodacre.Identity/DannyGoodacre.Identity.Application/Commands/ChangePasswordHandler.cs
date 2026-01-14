using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

internal sealed class ChangePasswordHandler(ILogger<ChangePasswordHandler> logger,
                                            IUserContext userContext,
                                            IUserManager<IdentityUser> userManager)
    : CommandHandler<ChangePasswordRequest>(logger), IChangePassword
{

    protected override string CommandName => "Change Password";

    protected override void Validate(ValidationState validationState, ChangePasswordRequest command)
    {
        validationState.IsNotNullEmptyOrWhitespace(nameof(command.CurrentPassword),  command.CurrentPassword);

        validationState.IsNotNullEmptyOrWhitespace(nameof(command.NewPassword), command.NewPassword);
    }

    protected async override Task<Result> InternalExecuteAsync(ChangePasswordRequest command, CancellationToken cancellationToken)
    {
        var userId = userContext.GetUserId();

        if (userId is null)
        {
            return Result.DomainError("User not found");
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.DomainError("User not found");
        }

        return await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
    }

    public Task<Result> ExecuteAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        }, cancellationToken);
}

internal sealed record ChangePasswordRequest : ICommandRequest
{
    public required string CurrentPassword { get; init; }

    public required string NewPassword { get; init; }
}

public interface IChangePassword
{
    Task<Result> ExecuteAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
