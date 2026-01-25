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
    : CommandHandler<Login>(logger), ILogin
{

    protected override string CommandName => "Login";

    protected override void Validate(ValidationState validationState, Login command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Username, nameof(command.Username));

        validationState.IsNotNullEmptyOrWhitespace(command.Password, nameof(command.Password));
    }

    protected async override Task<Result> InternalExecuteAsync(Login command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByUsernameAsync(command.Username);

        if (user is null)
        {
            return Result.DomainError("User not found.");
        }

        if (!await userManager.IsUserConfirmedAsync(user))
        {
            return Result.DomainError("User not confirmed.");
        }

        return await signInManager.PasswordSignInAsync(command.Username, command.Password);
    }

    public Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new Login
        {
            Username = username,
            Password = password
        }, cancellationToken);
}

internal sealed record Login : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

public interface ILogin
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}
