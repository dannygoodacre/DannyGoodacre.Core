using System.Reflection.Metadata.Ecma335;
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

internal sealed record RegisterNewUser : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class CreateUserHandler(ILogger<CreateUserHandler> logger,
                                        IUnitOfWork unitOfWork,
                                        IUserStore<IdentityUser> userStore,
                                        IUserManager<IdentityUser> userManager)
    : TransactionCommandHandler<RegisterNewUser, UserInfoResponse>(logger, unitOfWork), IRegisterNewUser
{
    protected override string CommandName => "Create User";

    protected override void Validate(ValidationState validationState, RegisterNewUser command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Username,  nameof(command.Username));

        validationState.IsNotNullEmptyOrWhitespace(command.Password, nameof(command.Password));
    }

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(RegisterNewUser command, CancellationToken cancellationToken)
    {
        var user = new IdentityUser();

        await userStore.SetUsernameAsync(user, command.Username, cancellationToken);

        var result = await userManager.CreateAsync(user, command.Password);

        if (!result.IsSuccess)
        {
            return result.ToResult<UserInfoResponse>();
        }

        // TODO
        return Result.Success(new UserInfoResponse
        {
            UserId = null,
            Username = null,
            IsApproved = false
        });
    }

    public Task<Result<UserInfoResponse>> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new RegisterNewUser
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
