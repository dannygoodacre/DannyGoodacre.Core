using System.Security.Claims;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Identity.Commands;

internal sealed class ChangePasswordHandler(ILogger<ChangePasswordHandler> logger,
                                          IHttpContextAccessor httpContextAccessor,
                                          UserManager<ApplicationUser> userManager)
    : CommandHandler<ChangePasswordRequest>(logger), IChangePassword
{

    protected override string CommandName => "Change Password";

    protected override void Validate(ValidationState validationState, ChangePasswordRequest changePasswordRequest)
    {
        // TODO
    }

    protected async override Task<Result> InternalExecuteAsync(ChangePasswordRequest command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity is null || !httpContext.User.Identity.IsAuthenticated)
        {
            return Result.DomainError("User not found");
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Result.DomainError("User not found");
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.DomainError("User not found");
        }

        var result = await userManager.ChangePasswordAsync(user, command.OldPassword, command.NewPassword);

        return result.Succeeded
            ? Result.Success()
            : Result.DomainError(result.ToString());
    }

    public Task<Result> ExecuteAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword
        }, cancellationToken);
}

internal sealed record ChangePasswordRequest : ICommand
{
    public required string OldPassword { get; init; }

    public required string NewPassword { get; init; }
}

internal interface IChangePassword
{
    Task<Result> ExecuteAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
