using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Identity.Commands;

public sealed class LogoutHandler(ILogger<LogoutHandler> logger,
                                  SignInManager<ApplicationUser> signInManager)
    : CommandHandler<LogoutRequest>(logger), ILogout
{

    protected override string CommandName => "Logout";

    protected async override Task<Result> InternalExecuteAsync(LogoutRequest command, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();

        return Result.Success();
    }

    public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        =>  ExecuteAsync(new LogoutRequest(), cancellationToken);
}

public sealed record LogoutRequest : ICommand;

public interface ILogout
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken = default);
}
