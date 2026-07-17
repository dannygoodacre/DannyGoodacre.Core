using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddUser
{
    Task<Result<UserInfo>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed record AddUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class AddUserHandler(ILogger<AddUserHandler> logger,
                                     IStateUnit stateUnit,
                                     IPasswordValidatorService passwordValidatorService,
                                     IHashingService hashingService,
                                     IUserRepository repository)
    : StateCommandHandler<AddUserCommand, UserInfo>(logger, stateUnit), IAddUser
{
    protected override string CommandName => "Add User";

    protected override void Validate(ValidationState state, AddUserCommand command)
        => passwordValidatorService.IsPasswordValid(state, command.Password);

    protected async override Task<Result<UserInfo>> InternalExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default)
    {
        bool isUsernameTaken = await repository.ExistsAsync(command.Username, cancellationToken);

        if (isUsernameTaken)
        {
            return Conflict("Username already taken");
        }

        User user = new()
        {
            PublicId = Guid.NewGuid(),
            Username = command.Username,
            IsApproved = false,
            PasswordHash = hashingService.Hash(command.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        repository.Add(user);

        return Success(user.ToUserInfoResponse());
    }

    public Task<Result<UserInfo>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
