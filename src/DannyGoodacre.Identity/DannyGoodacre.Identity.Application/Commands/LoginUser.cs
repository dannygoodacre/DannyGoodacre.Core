using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface ILoginUser
{
    public Task<Result<string>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed record LoginUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class LoginUserHandler(ILogger<LoginUserHandler> logger,
                                       IUnitOfWork unitOfWork,
                                       IUserRepository repository,
                                       IHashingService hashingService)
    : PersistenceCommandHandler<LoginUserCommand, string>(logger, unitOfWork), ILoginUser
{

    protected override string CommandName => "Login User";

    protected async override Task<Result<string>> InternalExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetForUpdateAsync(command.Username, cancellationToken);

        if (user is null)
        {
            return Result.NotFound();
        }

        if (!user.IsApproved)
        {
            return Result.Failed("Not approved");
        }

        if (!hashingService.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failed("Incorrect password");
        }

        user.SecurityStamp = Guid.NewGuid().ToString();
        user.LastLogin = DateTime.UtcNow;

        return Result.Success(user.SecurityStamp);
    }

    public Task<Result<string>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
        => ExecuteAsync(new LoginUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
