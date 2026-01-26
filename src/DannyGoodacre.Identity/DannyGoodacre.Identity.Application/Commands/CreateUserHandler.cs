using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IRegisterNewUser
{
    Task<Result<UserInfoResponse>> ExecuteAsync(string username, string password, CancellationToken cancellationToken);
}

internal sealed record RegisterNewUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class CreateUserHandler(ILogger<CreateUserHandler> logger,
                                        IUnitOfWork unitOfWork,
                                        IUserStore<IdentityUser> userStore,
                                        IUserManager<IdentityUser> userManager)
    : TransactionCommandHandler<RegisterNewUserCommand, UserInfoResponse>(logger, unitOfWork), IRegisterNewUser
{
    protected override string CommandName => "Create User";

    protected override void Validate(ValidationState validationState, RegisterNewUserCommand command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Username,  nameof(command.Username));

        validationState.IsNotNullEmptyOrWhitespace(command.Password, nameof(command.Password));
    }

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(RegisterNewUserCommand command, CancellationToken cancellationToken)
    {
        var user = new IdentityUser();

        await userStore.SetUsernameAsync(user, command.Username, cancellationToken);

        var result = await userManager.CreateAsync(user, command.Password);

        return result.IsSuccess
            ? Result.Success(user.ToUserInfoResponse())
            : result.ToResult<UserInfoResponse>();
    }

    public Task<Result<UserInfoResponse>> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new RegisterNewUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
