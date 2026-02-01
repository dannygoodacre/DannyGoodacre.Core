using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Core;
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
                                        IUnitOfWork context,
                                        IPasswordValidatorService passwordValidatorService,
                                        IHashingService hashingService,
                                        IUserRepository repository)
    : PersistenceCommandHandler<CreateUserCommand, UserInfo>(logger, context), ICreateUser
{
    protected override string CommandName => "Create User";

    protected override void Validate(ValidationState validationState, CreateUserCommand command)
        => passwordValidatorService.IsPasswordValid(validationState, command.Password);

    protected override Task<Result<UserInfo>> InternalExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Username = command.Username,
            IsApproved = false,
            PasswordHash = hashingService.Hash(command.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        repository.Add(user);

        return Task.FromResult(Result.Success(user.ToUserInfoResponse()));
    }

    public Task<Result<UserInfo>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
        => ExecuteAsync(new CreateUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
