using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Core.Identity.Commands;

public sealed class LoginHandler(ILogger<LoginHandler> logger,
                                 IOptions<IdentityOptions> options,
                                 UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager)
    : CommandHandler<LoginRequest>(logger), ILogin
{

    protected override string CommandName => "Login";

    protected override void Validate(ValidationState validationState, LoginRequest command)
    {
        // TODO: username and password must not be null, empty, whitespace.
    }

    protected async override Task<Result> InternalExecuteAsync(LoginRequest command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(command.Username);

        if (user is null)
        {
            return Result.DomainError("User not found.");
        }

        if (options.Value.SignIn.RequireConfirmedAccount && !await userManager.IsEmailConfirmedAsync(user))
        {
            return Result.DomainError("User not confirmed.");
        }

        var result = await signInManager.PasswordSignInAsync(command.Username, command.Password, false, false);

        return result.Succeeded
            ? Result.Success()
            : Result.DomainError(result.ToString());
    }

    public Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new LoginRequest
        {
            Username = username,
            Password = password
        }, cancellationToken);
}

public sealed record LoginRequest : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

public interface ILogin
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}
