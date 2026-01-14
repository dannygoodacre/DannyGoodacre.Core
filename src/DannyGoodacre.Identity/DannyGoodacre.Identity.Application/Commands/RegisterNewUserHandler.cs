using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

internal sealed class RegisterNewUserHandler(ILogger<RegisterNewUserHandler> logger,
                                             IUnitOfWork unitOfWork,
                                             IUserStore<IdentityUser> userStore,
                                             IUserManager<IdentityUser> userManager)
    : TransactionCommandHandler<RegisterNewUserRequest>(logger, unitOfWork), IRegisterNewUser
{
    protected override string CommandName => "Register New User";

    protected override void Validate(ValidationState validationState, RegisterNewUserRequest command)
    {
        validationState.IsNotNullEmptyOrWhitespace(nameof(command.Username),  command.Username);

        validationState.IsNotNullEmptyOrWhitespace(nameof(command.Password), command.Password);
    }

    protected async override Task<Result> InternalExecuteAsync(RegisterNewUserRequest command, CancellationToken cancellationToken)
    {
        var user = new IdentityUser();

        await userStore.SetUsernameAsync(user, command.Username, cancellationToken);

        return await userManager.CreateAsync(user, command.Password);
    }

    public Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new RegisterNewUserRequest
        {
            Username = username,
            Password = password
        }, cancellationToken);
}

internal sealed record RegisterNewUserRequest : ICommandRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

public interface IRegisterNewUser
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken);
}
