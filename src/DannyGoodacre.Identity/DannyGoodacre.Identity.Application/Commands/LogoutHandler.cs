using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

internal sealed class LogoutHandler(ILogger<LogoutHandler> logger, ISignInManager signInManager)
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

internal sealed record LogoutRequest : ICommandRequest;

public interface ILogout
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken = default);
}
