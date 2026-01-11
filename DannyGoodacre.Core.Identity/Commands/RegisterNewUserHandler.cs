using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Identity.Commands;

public sealed class RegisterNewUserHandler(ILogger<RegisterNewUserHandler> logger,
                                           IUnitOfWork unitOfWork,
                                           ITransactionProvider transactionProvider,
                                           IUserStore<ApplicationUser> userStore,
                                           UserManager<ApplicationUser> userManager)
    : TransactionCommandHandler<RegisterNewUserRequest>(logger, unitOfWork), IRegisterNewUser
{
    protected override string CommandName => "Register New User";

    protected override void Validate(ValidationState validationState, RegisterNewUserRequest command)
    {
        // TODO: username and password must not be null, empty, whitespace.
    }

    protected async override Task<Result> InternalExecuteAsync(RegisterNewUserRequest command, CancellationToken cancellationToken)
    {
        await using var transaction = await transactionProvider.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser();

        await userStore.SetUserNameAsync(user, command.Username, cancellationToken);

        var result = await userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            return Result.DomainError(result.ToString());
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    public Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        => ExecuteAsync(new RegisterNewUserRequest
        {
            Username = username,
            Password = password
        }, cancellationToken);
}

public sealed record RegisterNewUserRequest : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

public interface IRegisterNewUser
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken);
}
