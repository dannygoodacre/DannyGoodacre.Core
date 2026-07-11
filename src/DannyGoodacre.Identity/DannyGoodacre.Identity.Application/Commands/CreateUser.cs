using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Core;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface ICreateUser
{
    Task<Result<UserInfo>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed record CreateUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class CreateUserHandler(ILogger<CreateUserHandler> logger,
                                        IStateUnit stateUnit,
                                        IPasswordValidatorService passwordValidatorService,
                                        IHashingService hashingService,
                                        IUserRepository repository)
    : StateCommandHandler<CreateUserCommand, UserInfo>(logger, stateUnit), ICreateUser
{
    protected override string CommandName => "Create User";

    protected override void Validate(ValidationState state, CreateUserCommand command)
        => passwordValidatorService.IsPasswordValid(state, command.Password);

    protected async override Task<Result<UserInfo>> InternalExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        bool isUsernameTaken = await repository.ExistsAsync(command.Username, cancellationToken);

        if (isUsernameTaken)
        {
            return DomainError("Username already taken");
        }

        User user = new()
        {
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
        => ExecuteAsync(new CreateUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
