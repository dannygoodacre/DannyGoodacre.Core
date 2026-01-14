using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

internal sealed class LoginHandler(ILogger<LoginHandler> logger,
                                   IUserManager<IdentityUser> userManager,
                                   ISignInManager signInManager)
    : CommandHandler<LoginRequest>(logger), ILogin
{

    protected override string CommandName => "Login";

    protected override void Validate(ValidationState validationState, LoginRequest command)
    {
        validationState.IsNotNullEmptyOrWhitespace(nameof(command.Username),  command.Username);

        validationState.IsNotNullEmptyOrWhitespace(nameof(command.Password), command.Password);
    }

    protected async override Task<Result> InternalExecuteAsync(LoginRequest command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(command.Username);

        if (user is null)
        {
            return Result.DomainError("User not found.");
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            return Result.DomainError("User not confirmed.");
        }

        return await signInManager.PasswordSignInAsync(command.Username, command.Password);
    }

    public Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new LoginRequest
        {
            Username = username,
            Password = password
        }, cancellationToken);
}

internal sealed record LoginRequest : ICommandRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

public interface ILogin
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}
