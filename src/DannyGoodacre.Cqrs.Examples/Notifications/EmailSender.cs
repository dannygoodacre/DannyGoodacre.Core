using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs.Examples.Notifications;

public interface IEmailSender
{
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default);
}

// A stub that logs instead of sending. The log entry has no name or email address, so it contains no personal data.
public sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        LogWelcomeEmailSent();

        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Information, "Welcome email sent")]
    partial void LogWelcomeEmailSent();
}
