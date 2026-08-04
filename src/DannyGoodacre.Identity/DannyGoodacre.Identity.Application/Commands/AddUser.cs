using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddUser
{
    Task<Result<UserInfoResponse>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed record AddUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class AddUserHandler(ILogger<AddUserHandler> logger,
                                     IStateUnit stateUnit,
                                     IPasswordValidatorService passwordValidatorService,
                                     IUserRepository repository,
                                     IPasswordHashingService hashingService)
    : StateCommandHandler<AddUserCommand, UserInfoResponse>(logger, stateUnit), IAddUser
{
    protected override string CommandName => "Add User";

    protected override void Validate(ValidationState state, AddUserCommand command)
        => passwordValidatorService.IsPasswordValid(state, command.Password);

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsAsync(command.Username, cancellationToken))
        {
            return Conflict("Username already taken");
        }

        string passwordHash = hashingService.Hash(command.Password);

        User user = repository.Add(User.CreateNew(command.Username, passwordHash));

        return Success(user.ToUserInfoResponse());
    }

    public Task<Result<UserInfoResponse>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
