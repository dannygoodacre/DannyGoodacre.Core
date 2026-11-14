using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Onboarding;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs.Examples.Auditing;

// An abstract generic base hook. AddCqrsHandlers registers only the closed hooks derived from it, because its startup
// checks need the command type: a hook must target a state command whose handler has a matching signature.
// A non-abstract open generic hook would make AddCqrsHandlers throw at startup.
public abstract class AuditHook<TCommand, TResult>(ILogger<TCommand> logger) : IPostCommitHook<TCommand, TResult>
    where TCommand : ICommand
{
    public Task AfterCommitAsync(TCommand command, IResult<TResult> result, CancellationToken cancellationToken = default)
    {
        logger.LogCommitted(typeof(TCommand).Name);

        return Task.CompletedTask;
    }
}

// A child command's hooks wait for the outermost commit, so onboarding logs both commands after its transaction
// commits, and neither if it rolls back.
public sealed class RegisterCustomerAuditHook(ILogger<RegisterCustomerCommand> logger)
    : AuditHook<RegisterCustomerCommand, Guid>(logger);

public sealed class OnboardCustomerAuditHook(ILogger<OnboardCustomerCommand> logger)
    : AuditHook<OnboardCustomerCommand, Guid>(logger);

internal static partial class AuditLog
{
    [LoggerMessage(LogLevel.Information, "Audit: '{CommandName}' committed")]
    public static partial void LogCommitted(this ILogger logger, string commandName);
}
